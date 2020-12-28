using System.IO;

namespace InflatablePalace.Cryptography.OpenPgp.Packet
{
    class DsaSecretBcpgKey : BcpgKey
    {
        private MPInteger x;

        public DsaSecretBcpgKey(Stream bcpgIn)
        {
            this.x = new MPInteger(bcpgIn);
        }

        public DsaSecretBcpgKey(MPInteger x)
        {
            this.x = x;
        }

        public override void Encode(Stream bcpgOut)
        {
            x.Encode(bcpgOut);
        }

        public MPInteger X => x;
    }
}