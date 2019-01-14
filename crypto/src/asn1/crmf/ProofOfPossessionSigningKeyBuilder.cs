﻿using System;
using System.Collections.Generic;
using System.Text;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Operators;
using Org.BouncyCastle.Crypto.Paddings;

namespace Org.BouncyCastle.Asn1.Crmf
{
    public class ProofOfPossessionSigningKeyBuilder
    {
        private CertRequest _certRequest;
        private SubjectPublicKeyInfo _pubKeyInfo;
        private GeneralName _name;
        private PKMacValue _publicKeyMAC;

        public ProofOfPossessionSigningKeyBuilder(CertRequest certRequest)
        {
            this._certRequest = certRequest;
        }


        public ProofOfPossessionSigningKeyBuilder(SubjectPublicKeyInfo pubKeyInfo)
        {
            this._pubKeyInfo = pubKeyInfo;
        }

        public ProofOfPossessionSigningKeyBuilder setSender(GeneralName name)
        {
            this._name = name;

            return this;
        }

        public ProofOfPossessionSigningKeyBuilder setPublicKeyMac(PkMacFactory generator, char[] password)
        {
            IStreamCalculator calc = generator.CreateCalculator();
            byte[] d = _pubKeyInfo.GetDerEncoded();
            calc.Stream.Write(d, 0, d.Length);
            calc.Stream.Flush();
            calc.Stream.Close();


            this._publicKeyMAC = new PKMacValue(
                (AlgorithmIdentifier)generator.AlgorithmDetails,
                new DerBitString(((DefaultMacAndDigestResult)calc.GetResult()).MacResult));

            return this;
        }

        public PopoSigningKey build(ISignatureFactory signer)
        {
            if (_name != null && _publicKeyMAC != null)
            {
                throw new InvalidOperationException("name and publicKeyMAC cannot both be set.");
            }

            PopoSigningKeyInput popo;
            byte[] b;
            IStreamCalculator calc = signer.CreateCalculator();
            if (_certRequest != null)
            {
                popo = null;
                b = _certRequest.GetDerEncoded();
                calc.Stream.Write(b, 0, b.Length);

            }
            else if (_name != null)
            {
                popo = new PopoSigningKeyInput(_name, _pubKeyInfo);
                b = popo.GetDerEncoded();
                calc.Stream.Write(b, 0, b.Length);
            }
            else
            {
                popo = new PopoSigningKeyInput(_publicKeyMAC, _pubKeyInfo);
                b = popo.GetDerEncoded();
                calc.Stream.Write(b, 0, b.Length);
            }

            calc.Stream.Flush();
            calc.Stream.Close();
            DefaultSignatureResult res = (DefaultSignatureResult)calc.GetResult();
            return new PopoSigningKey(popo, (AlgorithmIdentifier)signer.AlgorithmDetails, new DerBitString(res.Collect()));
        }


    }
}
