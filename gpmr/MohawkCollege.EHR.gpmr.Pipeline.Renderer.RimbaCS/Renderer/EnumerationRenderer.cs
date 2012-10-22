/* 
 * Copyright 2008-2012 Mohawk College of Applied Arts and Technology
 * 
 * Licensed under the Apache License, Version 2.0 (the "License"); you 
 * may not use this file except in compliance with the License. You may 
 * obtain a copy of the License at 
 * 
 * http://www.apache.org/licenses/LICENSE-2.0 
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
 * WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the 
 * License for the specific language governing permissions and limitations under 
 * the License.
 * 
 * User: Justin Fyfe
 * Date: 01-09-2009
 */
using System;
using System.Collections.Generic;
using System.Text;
using MohawkCollege.EHR.gpmr.Pipeline.Renderer.RimbaCS.Interfaces;
using MohawkCollege.EHR.gpmr.Pipeline.Renderer.RimbaCS.Attributes;
using System.IO;
using MohawkCollege.EHR.gpmr.COR;
using MohawkCollege.EHR.gpmr.Pipeline.Renderer.RimbaCS.HeuristicEngine;
using System.Diagnostics;

namespace MohawkCollege.EHR.gpmr.Pipeline.Renderer.RimbaCS.Renderer
{
    /// <summary>
    /// Renders enumerations
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Renderer")]
    [FeatureRenderer(Feature = typeof(MohawkCollege.EHR.gpmr.COR.ValueSet), IsFile = true)]
    [FeatureRenderer(Feature = typeof(MohawkCollege.EHR.gpmr.COR.CodeSystem), IsFile = true)]
    [FeatureRenderer(Feature = typeof(MohawkCollege.EHR.gpmr.COR.ConceptDomain), IsFile = true)]
    public class EnumerationRenderer : IFeatureRenderer
    {
        #region IFeatureRenderer Members

        /// <summary>
        /// The list of files currently rendered
        /// </summary>
        private static List<String> m_renderedFiles = new List<string>(100);

        /// <summary>
        /// Enumerations marked for use
        /// </summary>
        private static List<Enumeration> m_markedForUse = new List<Enumeration>(10);

        /// <summary>
        /// Mark as used
        /// </summary>
        /// <param name="enu"></param>
        /// <returns></returns>
        public static bool MarkAsUsed(Enumeration enu)
        {

            // This will check the literal count against bound vocab sets
            // if the enu is currently a concept domain
            if (enu is ConceptDomain && (enu as ConceptDomain).ContextBinding != null)
                enu = (enu as ConceptDomain).ContextBinding[0];
            
            if(!m_markedForUse.Contains(enu))
                m_markedForUse.Add(enu);
            return true;
        }

        /// <summary>
        /// Determine if we'll render this or not
        /// </summary>
        public static string WillRender(Enumeration enu)
        {


            if (!String.IsNullOrEmpty(Datatypes.GetBuiltinVocabulary(enu.Name)))
                return enu.Name;

            // This will check the literal count against bound vocab sets
            // if the enu is currently a concept domain
            if (enu is ConceptDomain && (enu as ConceptDomain).ContextBinding != null && (enu as ConceptDomain).ContextBinding.Count == 1)
                enu = (enu as ConceptDomain).ContextBinding[0];
            else if (enu is ConceptDomain && (enu as ConceptDomain).ContextBinding != null && (enu as ConceptDomain).ContextBinding.Count > 1) // HACK: If there is more than one context binding create a new value set, clear the binding and then re-bind
            {
                // Create the VS
                ValueSet vsNew = new ValueSet()
                {
                    Name = String.Format("{0}AutoGen", enu.Name),
                    BusinessName = enu.BusinessName,
                    Documentation = new Documentation()
                    {
                        Description = new List<string>(new string[] { 
                            String.Format("Value set has automatically been generated by GPMR to allow binding to ConceptDomain '{0}'", enu.Name)
                        }),
                        Rationale = new List<string>()
                    },
                    Id = enu.Id,
                    Literals = new List<Enumeration.EnumerationValue>(),
                    MemberOf = enu.MemberOf,
                    OwnerRealm = enu.OwnerRealm
                };
                    
                // Add literals and documentation
                vsNew.Documentation.Rationale.Add(String.Format("GPMR can normally only redirect context bindings from a concept domain if only 1 is present, however this concept domain has '{0}' present. This value set is a union of content from:", (enu as ConceptDomain).ContextBinding.Count));
                foreach (Enumeration vs in (enu as ConceptDomain).ContextBinding)
                {

                    // If any of the context binding codes are not to be rendered do not render any of them
                    if (WillRender(vs) == String.Empty)
                        return String.Empty;

                    // Output rationale
                    vsNew.Documentation.Rationale.Add(String.Format("<p>- {0} ({1})</p>", vs.Name, vs.EnumerationType));
                    // Add literals
                    vsNew.Literals.AddRange(vs.GetEnumeratedLiterals());
                }
                
                // Now fire parse to add to the domain
                vsNew.FireParsed();

                // Replace the context bindings
                (enu as ConceptDomain).ContextBinding.Clear();
                (enu as ConceptDomain).ContextBinding.Add(vsNew);

                // redirect
                enu = vsNew;

            }
            else if (enu is ConceptDomain)
                return String.Empty;

            // Partial enumerations or suppressed enumerations are not to be included
            if (enu.IsPartial && !RimbaCsRenderer.RenderPartials)
                return String.Empty;

            // Too big
            if (enu.GetEnumeratedLiterals().Count > RimbaCsRenderer.MaxLiterals)
                return String.Empty;

            // Already has a preferred name?
            if (enu.Annotations != null && enu.Annotations.Exists(o => o is RenderAsAnnotation))
                return (enu.Annotations.Find(o => o is RenderAsAnnotation) as RenderAsAnnotation).RenderName;

            // Already being used
            if (m_markedForUse.Exists(o => o.Name == enu.Name && o.GetType() == enu.GetType()))
                return enu.Name;

            string name = enu.Name;

            if (enu.GetEnumeratedLiterals().Count > 0 && enu.GetEnumeratedLiterals().FindAll(l => !l.Annotations.Exists(o => o is SuppressBrowseAnnotation)).Count > 0 &&
                (RimbaCsRenderer.GenerateVocab || (!RimbaCsRenderer.GenerateVocab && enu is ValueSet)))
            {
                // Name collision? Resolve
                if (enu.MemberOf.Find(o => o.Name == enu.Name && o.GetType() != enu.GetType() &&
                    !(o is ConceptDomain)) != null)
                {
                    if (m_markedForUse.Exists(o => o.Name == enu.Name && o.GetType() != enu.GetType()))
                    {
                        name = String.Format("{0}1", enu.Name);
                        if (enu.Annotations == null)
                            enu.Annotations = new List<Annotation>();
                        enu.Annotations.Add(new RenderAsAnnotation() { RenderName = name });
                    }
                }
                return name; // don't process
            }
            return String.Empty;
        }

        /// <summary>
        /// Render the feature
        /// </summary>
        /// <param name="apiNs">The namespace of the owned API</param>
        /// <param name="f">The feature to render</param>
        /// <param name="OwnerNS">The namespace of the feature</param>
        /// <param name="tw">The textwriter to write data to</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1307:SpecifyStringComparison", MessageId = "System.String.StartsWith(System.String)"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.IO.StringWriter.#ctor"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object,System.Object)"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object)")]
        public void Render(string OwnerNS, string apiNs, MohawkCollege.EHR.gpmr.COR.Feature f, System.IO.TextWriter tw)
        {

            StringWriter sw = new StringWriter();

            // Make a strong typed reference to enumeration
            Enumeration enu = f as Enumeration;

            // enumeration is a concept domain? do the binding
            if (enu is ConceptDomain && (enu as ConceptDomain).ContextBinding != null)
                enu = (enu as ConceptDomain).ContextBinding[0];
            else if(enu is ConceptDomain)
                throw new InvalidOperationException("Won't render unbound concept domains");
            
            #region Usings

            // Validate Usings
            string[] usings = new string[] { "Attributes", "Interfaces", "DataTypes" };
            foreach (string s in usings)
                sw.WriteLine("using {1}.{0};", s, apiNs);
            sw.WriteLine("using System.ComponentModel;");
            #endregion

            sw.WriteLine("namespace {0}.Vocabulary {{", OwnerNS); // start ns

            // Generate the documentation
            if (DocumentationRenderer.Render(enu.Documentation, 1).Length == 0)
                sw.WriteLine("\t/// <summary>{0}</summary>", enu.BusinessName ?? "No Documentation Found");
            else
                sw.Write(DocumentationRenderer.Render(enu.Documentation, 1));


            // Generate the structure attribute
            sw.WriteLine("\t[Structure(Name = \"{0}\", CodeSystem = \"{1}\", StructureType = StructureAttribute.StructureAttributeType.{2})]", enu.Name, enu.ContentOid, enu.GetType().Name);
            sw.WriteLine("\t[Serializable]");

            string renderName = enu.Name;
            if (enu.Annotations != null && enu.Annotations.Exists(o => o is RenderAsAnnotation))
                renderName = (enu.Annotations.Find(o => o is RenderAsAnnotation) as RenderAsAnnotation).RenderName;

            // Generate enum
            sw.WriteLine("\tpublic enum {0} {{ ", Util.Util.MakeFriendly(renderName));

            List<String> rendered = new List<string>(),
                mnemonics = new List<string>();
            RenderLiterals(sw, enu, rendered, mnemonics,  enu.Literals);

            String tStr = sw.ToString();
            tStr = tStr.Remove(tStr.LastIndexOf(","));
            tStr += ("\r\n\t}");
            tStr += ("\r\n}"); // end ns

            // Write to tw
            tw.Write(tStr);
        }

        /// <summary>
        /// Render filter expression
        /// </summary>
        private string RenderFilterExpression(List<FilterExpression> list)
        {
            StringWriter sw = new StringWriter();

            sw.WriteLine("\t/// <list type=\"table\"><listheader><term></term><description>Expression</description></listheader>");
            foreach (var fe in list)
            {
                if (fe is TextFilterExpression)
                    sw.WriteLine("\t///\t<item><term>{0}, {1}</term><description>{2}</description>", fe.Operator, fe.Filter, (fe as TextFilterExpression).Text);
                else if (fe is GroupFilterExpression)
                    sw.WriteLine("\t///\t<item><term>{0}, {1}</term><description>\r\n{2}\r\n</description>", fe.Operator, fe.Filter, RenderFilterExpression((fe as GroupFilterExpression).SubExpressions));
                else
                    sw.WriteLine("\t///\t<item><term>{0}, {1}</term><description>{2} {3} from {4}</description>", fe.Operator, fe.Filter, fe.HeadCode ?? fe.HeadCodeReference.Name, fe.Property, fe.From ?? fe.FromReference.Name);
            }

            sw.WriteLine("/// </list>");
            return sw.ToString();
        }

        /// <summary>
        /// Render literals
        /// </summary>
        private void RenderLiterals(StringWriter sw, Enumeration enu, List<string> rendered, List<String> mnemonics, List<Enumeration.EnumerationValue> literals)
        {
            // Literals
            foreach (Enumeration.EnumerationValue ev in literals)
            {
                string bn = Util.Util.PascalCase(ev.BusinessName);
                string rendName = Util.Util.PascalCase(bn ?? ev.Name) ?? "__Unknown";
                
                // Already rendered, so warn and skip
                if (rendered.Contains(rendName) || mnemonics.Contains(ev.Name))
                    System.Diagnostics.Trace.WriteLine(String.Format("Enumeration value {0} already rendered, skipping", ev.BusinessName), "warn");
                else if(!ev.Annotations.Exists(o=>o is SuppressBrowseAnnotation))
                {
                    sw.Write(DocumentationRenderer.Render(ev.Documentation, 2));
                    if (DocumentationRenderer.Render(ev.Documentation, 2).Length == 0) // Documentation correction
                        sw.WriteLine("\t\t/// <summary>{0}</summary>", (ev.BusinessName ?? ev.Name).Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\r", "").Replace("\n", ""));

                    sw.WriteLine("\t\t[Enumeration(Value = \"{0}\", SupplierDomain = \"{1}\")]", ev.Name, ev.CodeSystem ?? enu.ContentOid);

                    // Annotations?
                    if (ev.Annotations != null && ev.Annotations.Find(o => o is SuppressBrowseAnnotation) != null)
                        sw.WriteLine("\t\t[EditorBrowsable(EditorBrowsableState.Never)]\r\n\t\t[Browsable(false)]");

                    // Business name
                    if (!String.IsNullOrEmpty(ev.BusinessName))
                        sw.WriteLine("\t\t[Description(\"{0}\")]", ev.BusinessName.Replace("\r", "").Replace("\n", ""));

                    if (rendered.Find(o => o.Equals(rendName)) != null) // .NET enumeration field will be the same, so render something different
                        sw.Write("\t\t{0}", Util.Util.PascalCase(rendName + "_" + ev.Name) ?? "__Unknown");
                    else
                        sw.Write("\t\t{0}", rendName);

                    sw.WriteLine(","); // Another literal follows

                    sw.Write("\r\n"); // Newline

                    rendered.Add(rendName); // Add to rendered list to keep track
                    mnemonics.Add(ev.Name);

                    
                }
                if (ev.RelatedCodes != null)
                    RenderLiterals(sw, enu, rendered, mnemonics, ev.RelatedCodes);
            }
        }

        /// <summary>
        /// Create a file name 
        /// </summary>
        /// <param name="f">The feature to create a file for</param>
        /// <param name="FilePath">The path of the file</param>
        /// <returns>The full name of the file to create</returns>
        public string CreateFile(MohawkCollege.EHR.gpmr.COR.Feature f, string FilePath)
        {
            string fileName = Util.Util.MakeFriendly(f.Name);

            // Render as
            if (f.Annotations != null && f.Annotations.Exists(o => o is RenderAsAnnotation))
                fileName = Util.Util.MakeFriendly((f.Annotations.Find(o => o is RenderAsAnnotation) as RenderAsAnnotation).RenderName);

            fileName = Path.ChangeExtension(Path.Combine(Path.Combine(FilePath, "Vocabulary"), fileName), ".cs");

            var enu = f as Enumeration;

            if (!String.IsNullOrEmpty(Datatypes.GetBuiltinVocabulary(enu.Name)))
                throw new InvalidOperationException("Enumeration is builtin to core library. Will not render");

            // Is this code system even used?
            if (!m_markedForUse.Exists(o=>o.GetType().Equals(f.GetType()) && o.Name == f.Name))
            {
                if (enu.GetEnumeratedLiterals().Count > RimbaCsRenderer.MaxLiterals)
                    throw new InvalidOperationException(String.Format("Enumeration '{2}' too large, enumeration has {0} literals, maximum allowed is {1}",
                        enu.GetEnumeratedLiterals().Count, RimbaCsRenderer.MaxLiterals, enu.Name));
                else
                    throw new InvalidOperationException("Enumeration is not used, or is an unbound concept domain!");
            }



            // First, if the feature is a value set we will always render it
            if (File.Exists(fileName) && !(f as Enumeration).OwnerRealm.EndsWith(RimbaCsRenderer.prefRealm))
                throw new InvalidOperationException("Enumeration has already been rendered from the preferred realm. Will not render this feature");

            return fileName;
        }

        #endregion
    }
}