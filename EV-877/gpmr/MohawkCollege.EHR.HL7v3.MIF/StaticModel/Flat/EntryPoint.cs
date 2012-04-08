/* 
 * Copyright 2008/2009 Mohawk College of Applied Arts and Technology
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
 **/
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace MohawkCollege.EHR.HL7v3.MIF.MIF10.StaticModel.Flat
{
    /// <summary>
    /// Entry point stereotype for a flat model
    /// </summary>
    [XmlType(TypeName = "EntryPoint", Namespace = "urn:hl7-org:v3/mif")]
    public class EntryPoint : EntryPointBase
    {
        private string className;

        /// <summary>
        /// The name of the class that is the entry point
        /// </summary>
        [XmlAttribute("className")]
        public string ClassName
        {
            get { return className; }
            set { className = value; }
        }
	
    }
}