﻿namespace Springburg.Cryptography.OpenPgp.Keys
{
    public enum S2kUsageTag : byte
    {
        None = 0x00,
        Checksum = 0xff,
        Sha1 = 0xfe
    }
}
