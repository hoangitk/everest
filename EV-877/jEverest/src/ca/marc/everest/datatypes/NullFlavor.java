/* 
 * Copyright 2008-2011 Mohawk College of Applied Arts and Technology
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
 * Date: 06-29-2011
 */
package ca.marc.everest.datatypes;

import ca.marc.everest.interfaces.*;
import ca.marc.everest.annotations.*;
/**
 * Indicates why a value is not present.
 */
@Structure(name= "NullFlavor", structureType = StructureType.CONCEPTDOMAIN)
public enum NullFlavor implements IEnumeratedVocabulary {

	/**
	 * Information was sought but not found (e.g., patient was asked but didn't know).
	 */
	AskedUnknown ("ASKU"),
	/**
	 * An actual value may exist, but it must be derived from the provided information (usually an EXPR generic data type extension will be used to convey the derivation expressionexpression ..
	 */
	Derived ("DER"),
	/**
	 * The value as represented in the instance is not a member of the set of permitted data values in the constrained value domain of a variable..
	 */
	Invalid ("INV"),
	/**
	 * There is information on this item available but it has not been provided by the sender due to security, privacy or other reasons. There may be an alternate mechanism for gaining access to this information. Note: using this null flavor does provide information that may be a breach of confidentiality, even though no detail data is provided. Its primary purpose is for those circumstances where it is necessary to inform the receiver that the information does exist without providing any detail. .
	 */
	Masked ("MASK"),
	/**
	 * Known to have no proper value (e.g., last menstrual period for a male)..
	 */
	NotApplicable ("NA"),
	/**
	 * This information has not been sought (e.g., patient was not asked).
	 */
	NotAsked ("NASK"),
	/**
	 * Information is not available at this time but it is expected that it will be available later..
	 */
	Unavailable ("NAV"),
	/**
	 * No information whatsoever can be inferred from this exceptional value. This is the most general exceptional value. It is also the default exceptional value..
	 */
	NoInformation ("NI"),
	/**
	 * Negative infinity of numbers..
	 */
	NegativeInfinity ("NINF"),
	/**
	 * The actual value is not an element in the value domain of a variable. (e.g., concept not provided by required code system)..
	 */
	Other ("OTH"),
	/**
	 * Positive infinity of numbers..
	 */
	PositiveInfinity ("PINF"),
	/**
	 * The specific quantity is not known, but is known to be non-zero and is not specified because it makes up the bulk of the material. 'Add 10mg of ingredient X, 50mg of ingredient Y, and sufficient quantity of water to 100mL.' The null flavor would be used to express the quantity of water. .
	 */
	SufficientQuantity ("QS"),
	/**
	 * The content is greater than zero, but too small to be quantified..
	 */
	Trace ("TRC"),
	/**
	 * The actual value has not yet been encoded within the approved valueset for the domain..
	 */
	UnEncoded ("UNC"),
	/**
	 *  A proper value is applicable, but not known..
	 */
	Unknown ("UNK");
	
	/**
	 * Creates a new instance of the NullFlavor class with the specified code
	 * @param code
	 */
	NullFlavor(String code)
	{
		this.m_code = code;
	}
	
	// Backing field for code
	private final String m_code;

	/**
	 * Gets the code system for the NullFlavor codeset
	 */
	public String getCodeSystem() { return "2.16.840.1.113883.5.1008"; }
	/**
	 * Get the code for the nullflavor
	 */
	public String getCode() { return this.m_code; }
	
}
