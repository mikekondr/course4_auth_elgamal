using System.Numerics;

namespace auth_elgamal.Models.Keys
{
    public class PublicKey
    {
        public BigInteger P { get; set; }
        public BigInteger Q { get; set; }
        public BigInteger G { get; set; }
        public BigInteger Y { get; set; }
    }

    public class PrivateKey
    {
        public BigInteger P { get; set; }
        public BigInteger Q { get; set; }
        public BigInteger G { get; set; }
        public BigInteger X { get; set; }
    }
}