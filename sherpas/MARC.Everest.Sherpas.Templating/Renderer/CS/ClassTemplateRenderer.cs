﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MARC.Everest.Sherpas.Template.Interface;
using MARC.Everest.Sherpas.Templating.Format;
using System.CodeDom;
using MARC.Everest.Attributes;
using System.Diagnostics;
using MARC.Everest.Sherpas.Interface;
using MARC.Everest.Connectors;

namespace MARC.Everest.Sherpas.Templating.Renderer.CS
{
    /// <summary>
    /// Class template renderer
    /// </summary>
    public class ClassTemplateRenderer : IArtifactRenderer
    {
        #region IArtifactRenderer Members

        /// <summary>
        /// Artifact type this renderer renders
        /// </summary>
        public Type ArtifactTemplateType
        {
            get { return typeof(ClassTemplateDefinition); }
        }

        /// <summary>
        /// Render the artifact
        /// </summary>
        public System.CodeDom.CodeTypeMemberCollection Render(RenderContext context)
        {
            var tpl = context.Artifact as ClassTemplateDefinition;

            // emit the enum
            CodeTypeDeclaration retVal = new CodeTypeDeclaration(tpl.Name);
            context.CurrentObject = retVal;
            retVal.IsClass = true;
            retVal.Attributes = MemberAttributes.Public;

            // Get the base class
            var structureAttribute = tpl.BaseClass.Type.GetCustomAttributes(typeof(StructureAttribute), false)[0] as StructureAttribute;
            
            // Add structure attribute
            retVal.CustomAttributes.Add(new CodeAttributeDeclaration(new CodeTypeReference(typeof(StructureAttribute)),
                new CodeAttributeArgument("Name", new CodePrimitiveExpression(structureAttribute.Name)),
                new CodeAttributeArgument("StructureType", new CodeFieldReferenceExpression(new CodeTypeReferenceExpression(typeof(MARC.Everest.Attributes.StructureAttribute.StructureAttributeType)), "MessageType")),
                new CodeAttributeArgument("Model", new CodePrimitiveExpression(structureAttribute.Model)),
                new CodeAttributeArgument("Publisher", new CodePrimitiveExpression(structureAttribute.Publisher)),
                new CodeAttributeArgument("IsEntryPoint", new CodePrimitiveExpression(structureAttribute.IsEntryPoint))
            ));

            if(tpl.Id != null)
                foreach(var id in tpl.Id)
                    retVal.CustomAttributes.Add(new CodeAttributeDeclaration(new CodeTypeReference(typeof(TemplateAttribute)), new CodeAttributeArgument("TemplateId", new CodePrimitiveExpression(id))));

            retVal.Comments.AddRange(RenderUtils.RenderComments(tpl, String.Format("{0} is a template for <see cref=\"T:{1}\"/>", tpl.Name, tpl.BaseClass.Type.FullName)));

            // base class
            retVal.BaseTypes.Add(new CodeTypeReference(tpl.BaseClass.Type));

            // Initialization method
            CodeMemberMethod initializeInstanceMethod = new CodeMemberMethod()
            {
                Name = "InitializeInstance",
                ReturnType = new CodeTypeReference(typeof(void)),
                Attributes = MemberAttributes.Family
            },
            validateMethod = new CodeMemberMethod()
            {
                Name = "ValidateEx",
                ReturnType = new CodeTypeReference(typeof(IEnumerable<IResultDetail>)),
                Attributes = MemberAttributes.Public | MemberAttributes.Override
            };

            // Validate the method setup
            validateMethod.Statements.Add(new CodeVariableDeclarationStatement(new CodeTypeReference(typeof(List<IResultDetail>)), "retVal", new CodeCastExpression(new CodeTypeReference(typeof(List<IResultDetail>)),  new CodeMethodInvokeExpression(new CodeMethodReferenceExpression(new CodeBaseReferenceExpression(), "ValidateEx")))));
            foreach (var itm in tpl.Validation)
                validateMethod.Statements.AddRange(itm.ToCodeDomStatement(context));

            // Initialize method setup
            foreach (var itm in tpl.Initialize)
                initializeInstanceMethod.Statements.AddRange(itm.ToCodeDomStatement(context));
           
            retVal.Members.Add(initializeInstanceMethod);
            retVal.Members.Add(validateMethod);

            // Render the methods ... This should be interesting ... yikes!
            foreach (var itm in tpl.Templates)
            {
                var childContext = new RenderContext(context, itm, retVal);
                var renderer = childContext.GetRenderer();
                if (renderer == null)
                    Trace.TraceError("Could not find renderer for type '{0}'...", itm.GetType().Name);
                else
                    retVal.Members.AddRange(renderer.Render(childContext));

            }

            // Constructor to initialize instance
            var ctor = new CodeConstructor()
            {
                Attributes = MemberAttributes.Public
            };
            ctor.Comments.Add(new CodeCommentStatement(String.Format("<summary>Constructs a new instance of {0}</summary>", retVal.Name), true));
            ctor.Statements.Add(new CodeMethodInvokeExpression(new CodeThisReferenceExpression(), "InitializeInstance"));
            retVal.Members.Insert(0, ctor);

            // Generate a ctor for all mandatory elements
            var propNames = tpl.Templates.FindAll(o => o is PropertyTemplateDefinition && (o as PropertyTemplateDefinition).MinOccurs != "0");

            // Prop names for a ctor?
            if (propNames.Count > 0)
            {
                ctor = new CodeConstructor()
                {
                    Attributes = MemberAttributes.Public
                };
                
                ctor.Statements.Add(new CodeMethodInvokeExpression(new CodeThisReferenceExpression(), "InitializeInstance"));
                foreach (PropertyTemplateDefinition pn in propNames)
                {
                    //var prop = retVal.Members.OfType<CodeMemberProperty>().First(p => p.Name == pn.Name);

                    // Ensure that this already doesn't exist in initialize
                    var backingProp = retVal.Members.OfType<CodeMemberProperty>().FirstOrDefault(p => p.Name == pn.Name);
                    if (pn.Initialize.Count == 0 && pn.Property != null && backingProp != null && ctor.Parameters.OfType<CodeParameterDeclarationExpression>().Count(p=>p.Name == pn.TraversalName) == 0)
                    {
                        var codeTypeReference = backingProp.Type;
                        if (RenderUtils.EnumerableClassNames.Contains(codeTypeReference.BaseType))
                            codeTypeReference = backingProp.Type.TypeArguments[0];

                        ctor.Parameters.Add(new CodeParameterDeclarationExpression(codeTypeReference, pn.TraversalName));
                        if (codeTypeReference == backingProp.Type)
                            ctor.Statements.Add(new CodeAssignStatement(new CodePropertyReferenceExpression(new CodeThisReferenceExpression(), backingProp.Name), new CodeVariableReferenceExpression(pn.TraversalName)));
                        else
                        {
                            ctor.Statements.Add(new CodeAssignStatement(new CodePropertyReferenceExpression(new CodeThisReferenceExpression(), backingProp.Name), new CodeObjectCreateExpression(backingProp.Type)));
                            ctor.Statements.Add(new CodeMethodInvokeExpression(new CodePropertyReferenceExpression(new CodeThisReferenceExpression(), backingProp.Name), "Add", new CodeVariableReferenceExpression(pn.TraversalName)));
                        }
                    }
                }
                if(ctor.Parameters.Count > 0)
                    retVal.Members.Insert(1, ctor);
            }

            validateMethod.Statements.Add(new CodeMethodReturnStatement(new CodeVariableReferenceExpression("retVal")));


            // Add the formal constraints
            foreach (var fc in tpl.FormalConstraint)
            {
                if (fc.Instruction.Count == 0)
                    continue;

                CodeMemberMethod method = RenderUtils.RenderFormalConstraintValidator(fc, String.Format("{0}Constraint{1}", retVal.Name, tpl.FormalConstraint.IndexOf(fc)), new CodeTypeReference(retVal.Name), context);
                
                // Add the attribute 
                retVal.CustomAttributes.Add(new CodeAttributeDeclaration(new CodeTypeReference(typeof(FormalConstraintAttribute)),
                    new CodeAttributeArgument("Description", new CodePrimitiveExpression(fc.Message)),
                    new CodeAttributeArgument("CheckConstraintMethod", new CodePrimitiveExpression(method.Name))
                ));
                retVal.Members.Add(method);
            }

            // Add the interface for the IMessageTypeTemplate interface
            retVal.BaseTypes.Add(new CodeTypeReference(typeof(IMessageTypeTemplate)));

            // Add conditions to initialize properties
            return new CodeTypeMemberCollection(new CodeTypeMember[] { retVal });
        }

        #endregion
    }
}
