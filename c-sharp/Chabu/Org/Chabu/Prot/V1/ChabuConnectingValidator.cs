/*******************************************************************************
 * The MIT License (MIT)
 * Copyright (c) 2015 Frank Benoit, Stuttgart, Germany <fr@nk-benoit.de>
 * 
 * See the LICENSE.md or the online documentation:
 * https://docs.google.com/document/d/1Wqa8rDi0QYcqcf0oecD8GW53nMVXj3ZFSmcF81zAa8g/edit#heading=h.2kvlhpr5zi2u
 * 
 * Contributors:
 *     Frank Benoit - initial API and implementation
 *******************************************************************************/
namespace Org.Chabu.Prot.V1
{

    /**
     * For accepted, either returns null or a ChabuConnectionAcceptInfo with code set to 0.
     */
    public delegate ChabuConnectionAcceptInfo ChabuConnectingValidator(ChabuSetupInfo local, ChabuSetupInfo remote);
}