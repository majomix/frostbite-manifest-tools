using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FrostbiteManifestSystemTools.Model
{
    public class FrostbiteHeader
    {
        private readonly byte[] allowedSignature = { 0x0, 0xD1, 0xCE };
        private readonly IEnumerable<byte> allowedVersions = new byte[] { 0x01, 0x03 };
        private readonly string allowedHexValue = @"xa37dd45ffe100bfffcc9753aabac325f07cb3fa231144fe2e33ae4783feead2b8a73ff021fac326df0ef9753ab9cdf6573ddff0312fab0b0ff39779eaff312a4f5de65892ffee33a44569bebf21f66d22e54a22347efd375981188743afd99baacc342d88a99321235798725fedcbf43252669dade32415fee89da543bf23d4ex";

        private byte[] mySignature;
        private byte myVersion;
        private string myHexValue;

        public byte[] Signature
        {
            get { return mySignature; }
            set
            {
                if (!value.SequenceEqual(allowedSignature))
                {
                    throw new InvalidDataException();
                }
                else
                {
                    mySignature = value;
                }
            }
        }

        public byte Version
        {
            get { return myVersion; }
            set
            {
                if (!allowedVersions.Contains(value))
                {
                    throw new InvalidDataException();
                }
                else
                {
                    myVersion = value;
                }
            }
        }

        public string HexValue
        {
            get { return myHexValue; }
            set
            {
                if (!allowedHexValue.Equals(value))
                {
                    throw new InvalidDataException();
                }
                else
                {
                    myHexValue = value;
                }
            }
        }

        public byte[] XorKey { get; set; }
    }
}
