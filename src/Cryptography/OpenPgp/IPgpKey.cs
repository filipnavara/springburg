﻿namespace Springburg.Cryptography.OpenPgp
{
    interface IPgpKey
    {
        long KeyId { get; }
        bool IsMasterKey { get; }
    }
}
