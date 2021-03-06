using System;
using System.IO;
using System.Linq;
using System.Text;
using Springburg.Cryptography.OpenPgp;
using Springburg.Cryptography.OpenPgp.Packet;
using NUnit.Framework;
using Org.BouncyCastle.Utilities.Test;

namespace Org.BouncyCastle.Bcpg.OpenPgp.Tests
{
    [TestFixture]
    public class PgpSignatureTest
        : SimpleTest
    {
        private static readonly PgpSymmetricKeyAlgorithm[] PREFERRED_SYMMETRIC_ALGORITHMS
            = new PgpSymmetricKeyAlgorithm[] { PgpSymmetricKeyAlgorithm.Aes128, PgpSymmetricKeyAlgorithm.TripleDes };
        private static readonly PgpHashAlgorithm[] PREFERRED_HASH_ALGORITHMS
            = new PgpHashAlgorithm[] { PgpHashAlgorithm.Sha1, PgpHashAlgorithm.Sha256 };
        private static readonly PgpCompressionAlgorithm[] PREFERRED_COMPRESSION_ALGORITHMS
            = new PgpCompressionAlgorithm[] { PgpCompressionAlgorithm.ZLib };

        private TimeSpan TEST_EXPIRATION_TIME = TimeSpan.FromSeconds(10000);
        private const string TEST_USER_ID = "test user id";
        private static readonly byte[] TEST_DATA = Encoding.ASCII.GetBytes("hello world!\nhello world!\n");
        private static readonly byte[] TEST_DATA_WITH_CRLF = Encoding.ASCII.GetBytes("hello world!\r\nhello world!\r\n");

        private static readonly byte[] dsaKeyRing = Convert.FromBase64String(
            "lQHhBD9HBzURBACzkxRCVGJg5+Ld9DU4Xpnd4LCKgMq7YOY7Gi0EgK92gbaa6+zQ"
            + "oQFqz1tt3QUmpz3YVkm/zLESBBtC1ACIXGggUdFMUr5I87+1Cb6vzefAtGt8N5VV"
            + "1F/MXv1gJz4Bu6HyxL/ncfe71jsNhav0i4yAjf2etWFj53zK6R+Ojg5H6wCgpL9/"
            + "tXVfGP8SqFvyrN/437MlFSUEAIN3V6j/MUllyrZglrtr2+RWIwRrG/ACmrF6hTug"
            + "Ol4cQxaDYNcntXbhlTlJs9MxjTH3xxzylyirCyq7HzGJxZzSt6FTeh1DFYzhJ7Qu"
            + "YR1xrSdA6Y0mUv0ixD5A4nPHjupQ5QCqHGeRfFD/oHzD4zqBnJp/BJ3LvQ66bERJ"
            + "mKl5A/4uj3HoVxpb0vvyENfRqKMmGBISycY4MoH5uWfb23FffsT9r9KL6nJ4syLz"
            + "aRR0gvcbcjkc9Z3epI7gr3jTrb4d8WPxsDbT/W1tv9bG/EHawomLcihtuUU68Uej"
            + "6/wZot1XJqu2nQlku57+M/V2X1y26VKsipolPfja4uyBOOyvbP4DAwIDIBTxWjkC"
            + "GGAWQO2jy9CTvLHJEoTO7moHrp1FxOVpQ8iJHyRqZzLllO26OzgohbiPYz8u9qCu"
            + "lZ9Xn7QzRXJpYyBFY2hpZG5hIChEU0EgVGVzdCBLZXkpIDxlcmljQGJvdW5jeWNh"
            + "c3RsZS5vcmc+iFkEExECABkFAj9HBzUECwcDAgMVAgMDFgIBAh4BAheAAAoJEM0j"
            + "9enEyjRDAlwAnjTjjt57NKIgyym7OTCwzIU3xgFpAJ0VO5m5PfQKmGJRhaewLSZD"
            + "4nXkHg==");

        private static readonly string dsaPass = "hello world";

        private static readonly byte[] rsaKeyRing = Convert.FromBase64String(
              "lQIEBEBXUNMBBADScQczBibewnbCzCswc/9ut8R0fwlltBRxMW0NMdKJY2LF"
            + "7k2COeLOCIU95loJGV6ulbpDCXEO2Jyq8/qGw1qD3SCZNXxKs3GS8Iyh9Uwd"
            + "VL07nMMYl5NiQRsFB7wOb86+94tYWgvikVA5BRP5y3+O3GItnXnpWSJyREUy"
            + "6WI2QQAGKf4JAwIVmnRs4jtTX2DD05zy2mepEQ8bsqVAKIx7lEwvMVNcvg4Y"
            + "8vFLh9Mf/uNciwL4Se/ehfKQ/AT0JmBZduYMqRU2zhiBmxj4cXUQ0s36ysj7"
            + "fyDngGocDnM3cwPxaTF1ZRBQHSLewP7dqE7M73usFSz8vwD/0xNOHFRLKbsO"
            + "RqDlLA1Cg2Yd0wWPS0o7+qqk9ndqrjjSwMM8ftnzFGjShAdg4Ca7fFkcNePP"
            + "/rrwIH472FuRb7RbWzwXA4+4ZBdl8D4An0dwtfvAO+jCZSrLjmSpxEOveJxY"
            + "GduyR4IA4lemvAG51YHTHd4NXheuEqsIkn1yarwaaj47lFPnxNOElOREMdZb"
            + "nkWQb1jfgqO24imEZgrLMkK9bJfoDnlF4k6r6hZOp5FSFvc5kJB4cVo1QJl4"
            + "pwCSdoU6luwCggrlZhDnkGCSuQUUW45NE7Br22NGqn4/gHs0KCsWbAezApGj"
            + "qYUCfX1bcpPzUMzUlBaD5rz2vPeO58CDtBJ0ZXN0ZXIgPHRlc3RAdGVzdD6I"
            + "sgQTAQIAHAUCQFdQ0wIbAwQLBwMCAxUCAwMWAgECHgECF4AACgkQs8JyyQfH"
            + "97I1QgP8Cd+35maM2cbWV9iVRO+c5456KDi3oIUSNdPf1NQrCAtJqEUhmMSt"
            + "QbdiaFEkPrORISI/2htXruYn0aIpkCfbUheHOu0sef7s6pHmI2kOQPzR+C/j"
            + "8D9QvWsPOOso81KU2axUY8zIer64Uzqc4szMIlLw06c8vea27RfgjBpSCryw"
            + "AgAA");

        private static readonly string rsaPass = "2002 Buffalo Sabres";

        private static readonly byte[] nullPacketsSubKeyBinding = Convert.FromBase64String(
            "iDYEGBECAAAAACp9AJ9PlJCrFpi+INwG7z61eku2Wg1HaQCgl33X5Egj+Kf7F9CXIWj2iFCvQDo=");

        private static readonly byte[] okAttr = Convert.FromBase64String(
                "mQENBFOkuoMBCAC+8WcWLBZovlR5pLW4tbOoH3APia+poMEeTNkXKe8yAH0f"
              + "ZmTQgeXFBIizd4Ka1QETbayv+C6Axt6Ipdwf+3N/lqcOqg6PEwuIX4MBrv5R"
              + "ILMH5QyM3a3RlyXa7xES3I9t2VHiZvl15OrTZe67YNGtxlXyeawt6v/9d/a3"
              + "M1EaUzjN4H2EfI3P/VWpMUvQkn70996UKInOyaSB0hef/QS10jshG9DdgmLM"
              + "1/mJFRp8ynZOV4yGLnAdoEoPGG/HJZEzWfqOiwmWZOIrZIwedY1eKuMIhUGv"
              + "LTC9u+9X0h+Y0st5eb1pf8OLvrpRpEyHMrxXfj/V3rxom4d160ifGihPABEB"
              + "AAG0GndpdGggYXR0dHIgPGF0dHJAYXR0ci5uZXQ+iQE4BBMBAgAiBQJTpLqD"
              + "AhsDBgsJCAcDAgYVCAIJCgsEFgIDAQIeAQIXgAAKCRBCjbg0bKVgCXJiB/wO"
              + "6ksdrAy+zVxygFhk6Ju2vpMAOGnLl1nqBVT1mA5XiJu3rSiJmROLF2l21K0M"
              + "BICZfz+mjIwN56RZNzZnEmXk/E2+PgADV5VTRRsjqlyoeN/NrLWuTm9FyngJ"
              + "f96jVPysN6FzYRUB5Fuys57P+nu0RMoLGkHmQhp4L5hgNJTBy1SRnXukoIgJ"
              + "2Ra3EBQ7dBrzuWW1ycwU5acfOoxfcVqgXkiXaxgvujFChZGWT6djbnbbzlMm"
              + "sMKr6POKChEPWo1HJXXz1OaPsd75JA8bImgnrHhB3dHhD2wIqzQrtTxvraqz"
              + "ZWWR2xYZPltzBSlaAdn8Hf0GGBoMhutb3tJLzbAX0cybzJkBEAABAQAAAAAA"
              + "AAAAAAAAAP/Y/+AAEEpGSUYAAQEAAAEAAQAA/9sAQwAKBwcIBwYKCAgICwoK"
              + "Cw4YEA4NDQ4dFRYRGCMfJSQiHyIhJis3LyYpNCkhIjBBMTQ5Oz4+PiUuRElD"
              + "PEg3PT47/9sAQwEKCwsODQ4cEBAcOygiKDs7Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7"
              + "Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7/8AAEQgAkAB4AwEiAAIR"
              + "AQMRAf/EAB8AAAEFAQEBAQEBAAAAAAAAAAABAgMEBQYHCAkKC//EALUQAAIB"
              + "AwMCBAMFBQQEAAABfQECAwAEEQUSITFBBhNRYQcicRQygZGhCCNCscEVUtHw"
              + "JDNicoIJChYXGBkaJSYnKCkqNDU2Nzg5OkNERUZHSElKU1RVVldYWVpjZGVm"
              + "Z2hpanN0dXZ3eHl6g4SFhoeIiYqSk5SVlpeYmZqio6Slpqeoqaqys7S1tre4"
              + "ubrCw8TFxsfIycrS09TV1tfY2drh4uPk5ebn6Onq8fLz9PX29/j5+v/EAB8B"
              + "AAMBAQEBAQEBAQEAAAAAAAABAgMEBQYHCAkKC//EALURAAIBAgQEAwQHBQQE"
              + "AAECdwABAgMRBAUhMQYSQVEHYXETIjKBCBRCkaGxwQkjM1LwFWJy0QoWJDTh"
              + "JfEXGBkaJicoKSo1Njc4OTpDREVGR0hJSlNUVVZXWFlaY2RlZmdoaWpzdHV2"
              + "d3h5eoKDhIWGh4iJipKTlJWWl5iZmqKjpKWmp6ipqrKztLW2t7i5usLDxMXG"
              + "x8jJytLT1NXW19jZ2uLj5OXm5+jp6vLz9PX29/j5+v/aAAwDAQACEQMRAD8A"
              + "9moqtf30Gm2cl3cvtijGSa4a++LNlGStlZvKR0ZuBWkKU6nwomU4x3PQqK8g"
              + "uPinrEzYhhihX86ns/Ffia/XzElJUHOV4/rW/wBUqJXlZEe2i9j1iivMP+Ex"
              + "1q3+/KCw6gip4PiXdREC5tUkHcrwaTwlVbK4e1iekUVzmheNdO1ycWyK8U5G"
              + "drf410QOa55RcXaSNE09ULRRRUjCiiigAooooAKKKKAOY+IblfCN1g9cA/rX"
              + "h1fQPiXT4dU0o2dwXEcrclCARgE8ZB9K4J/AGkKeJr38ZU/+Ir0MLiIUoNSO"
              + "erTlJ3R54v3hXpfg3UdNGmrHPMsToOc9+KrQeBdAd2SS7vkYdPnX/wCIqy3g"
              + "fRoThb+9GP8AaQ/+yVdavRqxs2yYU5wdzH164t57+V7XHlZOCOh5rn5n5Ndr"
              + "J4U0xBt/tC8x16p/8RTP+EK0uRQ32q9IPfzE/wDiKuGKpRSSYnSm3c5/wjP5"
              + "XiKFywUDqScelevR6/pCR4k1S0DDqPOXI/WvPLjwdplpbtPG9zI6so2yspU5"
              + "YDoFHrW7pOmRWpEiqVyuPlHH41xYmPgpPmibU4uKszqY9f0aZtseq2bN6eeu"
              + "f51fVldQyMGU9CDkGueMCOpxtYe3NYWoabJJOZrWV7V1yFe1cxnH1HX8a57G"
              + "lz0CiuFg8U6rpjql2PtkXTMgCv8Agw4/MfjXU6VrthrCH7NKRIoy8LjDr+Hp"
              + "7jIosFzRooopDCiiigClqXKRD1c/+gtWPLFitnUfuRH/AG//AGUiqDKGFAzA"
              + "mFzG7rGhAJJyB604XtzGGjeAuD3GR2x260t1fTJf3EChAsLKo+XOcorZP/fV"
              + "Qm8lPXZ/3yKLCJDPIBsjUjIHUewFWoYWS2jDDBArPN1IQR8o/wCAirdvcERw"
              + "u33ZYkdgOgLKCcfnRYBL0f8AEvmz6x/+jUqxbzyCLCKoC92NRaiMWLkHhmj/"
              + "AB+dTWlarutdoIXI64oQETXJ25MbA9DsolCEY4zjpVswL5QXgMB1xWZMRDIy"
              + "woJn6HnAWmIzb+GZyyIisD0Vl4Nc5I0ulXSO8zQtnMTrkGM/71dVNpufnMkm"
              + "7Odwfmqd5CGi8tuQB0b5v51SEzf8M+Kl1QixvdqXoHysOFmA7j0PqPxHoOlr"
              + "xm5DROrRkxvGQVZOCpHQivSPCfiEa9px80gXlvhZ1Hf0Yex/mDRKNtQTN6ii"
              + "ioKKmoD9zGfSVf1OP61QrUuovOgZM4PBB9CDkH865PxJrVx4d057yS0inAcI"
              + "qq5TJJ+hoAqXg/4m9/8A9dU/9FR1CRUGlan/AG7Fcal9n+z+dNjy9+/btRV6"
              + "4GemelWiKoRHVuIf6Ha/9e0X/oC1VIrIt/FtxNGsFtoxk+zoITI1zhWKjbn7"
              + "vt0zSYzfvJSLAIennIB+p/pWtZy4hXmuQa71fUzGhtre1jR920MXLHGMk+2T"
              + "6da1oZb22ULM6FDwGCkHNFhGzNqCbjAmXkPGF7VJFAkEQHBNQWkMUcQIwc85"
              + "9fepJJeOtNIVyK4bg1jXjda0LiTg1k3b9atEsxr3qai0LWDoOvQXpYiEny5x"
              + "6oep/Dg/hT7s9ayLoZVs1VriPeQcjIorC8F37ah4Vs3kbdLCvkyexXjn3xg/"
              + "jRWBqb1ee/FqYLpun24P+snLMPoOK9Crzb4uKQumSfwl2H44qo7iexB4JQHR"
              + "wCMj7Q39K2roRRXTkqPLU8iuB8NFl8S6ftdgrSHIycH5T2rvb8b2uap6MS1R"
              + "DJcWsq7YUCt65J4rA0FUCHKjh2/9CNYfjDUSkS2lskrlHDTSR/8ALPjocUaH"
              + "4msUtVjCM0qLyqkAH8TyKSBnoELoOgFJf3VoITFcTBNy546gevtzXM6Rqd3f"
              + "akWadyigsYw3y+gAH410O/PDZHHcU7E3LWnXED2SC2nE0ajG4HJ/GpJJeOtY"
              + "lxYpJdxXMcssLxkE+SwXdj14qrf6jrP22SK0t4RFkFZZMYx/n8aANieXg1mX"
              + "MnWla5lKRCSMFmB8xoz8qHHvzg1TnlzVIRTuW61l3MyQRSTuNwjXdt9T2FXZ"
              + "3zWfcRpPG8Mn3JBtJ9PQ/nVCO7+Dl49z4f1BJG3Mt6XJ/wB5V/woqD4LwvDp"
              + "urK45W5VT9QtFYPc1Wx6VXDfFi0M3hmG6A5trhSfoRj/AAruaz9d01dY0O80"
              + "9v8AlvEVX2bt+uKFowZ4z4Zbd4h04/8ATRv/AEBq7+T53ufrXnXhffF4ls4J"
              + "QVkildWB7EKwNehwnfLcD/aFXLcUThGs5bDUpYrgFWZ2dGHR1J6ip57C0voR"
              + "HcQq6htwI+Ug4xkEVo+MJ0jksrYA+ZuMhPouMfzP6VnQyEqKqOqJejMmfSr/"
              + "AE8NNbzC6hjG7aQVlA/kcVueFtR+12Mrpceagk4Abdt4/rUiMeOeaqS6UhuV"
              + "ubSaWymxtdrbC+YvoR6+9FhHRPcCNGaRgiqNzFjgAVmya/pYkZftSnH8QQlT"
              + "9D3rmdbefT4o7KO6ne3ky+yV9xBB9euO+Kw2mfruNAj0OW8t/K837TB5eM7/"
              + "ADBjFVp3IAOQQwyCDkEexrz95W9vrirula1LYyiOQu9s2Q0YPT3GehpgdJK2"
              + "apzt8hottQgv1k8pZEeMZIYg5GcZyKjuFkkKQxKXklYKijqSeAKdwPUvhdbe"
              + "X4ZmutpH2y7eUZ9AAv8ANTRXSaJpqaPotnpyYP2eIKxHdv4j+JyaKwe5qi/R"
              + "RRSGeaeJ/Dx03x7Yavbr/o967eZj+GQI38xz+dXdPffczD1cVu+Lzi0tT6Tj"
              + "/wBBNc3oz7r5x6uKroIwPFt5BeazFbQKGa1BWSQdycfL+GP1qCCPgU3+yprC"
              + "/ltrpcSqxOezAnhge9aMNv04rRaIh7jEiNSSFLeF55c7I1LNjrgVcjt/alu9"
              + "O+12U1uSUEqFNyjlcjrRcVjzzVL6bU5xJIioqjCIo4Uf1NUDEfStiXTLizuH"
              + "tboL5qc7l6OvZhTTZ+1K4WMZoSe1NFuSelbP2M9xT47As2FXJp3FYqaUptJ2"
              + "fZu3IVwSR1r0L4f6FHqmsf2w8bC3sjhA2CGlx29duc/UisHQ/DlzreoiwtPl"
              + "24NxPjKwL/Vj2H9K9m07T7bStPhsbOPy4IV2qO/uT6knkmoky4otUUUVBYUU"
              + "UUAc54yP+hWv/XwB+hrntOTyNbSP+84rs9Z04ajaqu7a8bh0OMjI9a5O6gvo"
              + "b3zjZAuDwyOMfryKaegEHjZTYva6qV8yFf3MqKMsueQw9uDmq+nPZahGJLSd"
              + "Hz2zyKsXEOpagyC4IWOM5WNOmfUnuaxtT8NOJPtFoGt5uu6PjP4U0xNHSx2b"
              + "jtmrC2p/u1xEOr+J9MO1sXCj++OavxeO9Tj4m0vJ9jTuI09c8NrqUavGfKuI"
              + "/wDVyhc49iO4rnToV/A/lXCI5xkPGCFI/HvWhL491BhiLSufc1l6hrXiTVZQ"
              + "IALaPGOFyfc0gHzadBZxGW9nSFBydxp+nafPrEii0RrOyP3rmRfncf7Cn+Z/"
              + "Wo9K8NXEl0Lm+L3EgOQZTux9K7W0s5BgYNFwsbOg2tlpVilnYxCOMHJ7s7Hq"
              + "xPc1sqcjNZNnbsuM1qoMLUlD6KKKACiiigBCM1E9tG55UVNRQBWNlF2UVC+m"
              + "xP8Aw1fooAx5NDgfqg/KoG8N2p/5ZL+Vb9FAHPjw1ag/6pfyqZNBt06IPyra"
              + "ooAzU0qJOiirCWcadBVqigBixhegp1LRQAUUUUAf/9mJATgEEwECACIFAlOk"
              + "xL4CGwMGCwkIBwMCBhUIAgkKCwQWAgMBAh4BAheAAAoJEEKNuDRspWAJhi8I"
              + "AKhIemGlaKtuZxBA4bQcJOy/ZdGJriJuu3OQl2m6CAwxaGMncpxHFVTT6GqI"
              + "Vu4/b4SSwYP1pI24MqAkdEudjFSi15ByogPFpUoDJC44zrO64b/mv3L5iq1C"
              + "PY+VvgLMAdvA3Tsoj/rNYlD0fieBa9EF8BtoAkaA4X6pihNPGsVe0AxlJhQw"
              + "eMgLXwTjllJm1iWa/fEQvv5Uk01gzayH1TIwkNAJ0E8s6Ontu2szUHjFGRNA"
              + "llR5OJzt/loo9p53zWddFfxlCfn2w+smHyB4i+FfpQfFSMLnwew7wncHs6XE"
              + "PevLPcW66T3w2/oMd0fC7GwhnCiebDYjl8ymF+4b0N65AQ0EU6S6gwEIAOAC"
              + "NRzXH0dc5wwkucFdTMs1nxr16y+Kk3zF3R21OkHLHazXVC7ZP2HurTFGd5VP"
              + "Yd+vv0CrYHCjjMu0lIeMfTlpJswvJRBxVw8vIVLpOSqxtJS+zysE8/LpKw6i"
              + "ti51ydalhm6VYGPm+OAoAAO1pLriwR132caoye5vqxGKEUCmkaNLl8LCljyH"
              + "kMgL5nQr+7cerTcGd2MaC8Y5vQuZBpVVBZcVt004iP3bCJu2l2RKskIoSysC"
              + "68bqV4XLMnoVeM97VPdwdb0Y7tGXCW8YodN8ni43YOaQxfr7fHx8nyzQ5S8w"
              + "a701GKWcQqCb0DR1ngCRAgWLzj8HDlZoofPL8d0AEQEAAYkBHwQYAQIACQUC"
              + "U6S6gwIbDAAKCRBCjbg0bKVgCWPSB/wN9Z5ayWiox5xxouAQv4W2JZGPiqk8"
              + "nFF5fzSgQxV4Xo63IaC1bD8411pgRlj1aWtt8pvWjEW9WWxvyPnkz0xldErb"
              + "NRZ9482TknY0dsrbmg6jwLOlNvLhLVhWUWt+DkH20daVCADV/0p2/2OPodn+"
              + "MYnueL5ljoJxzTO84WMz1u7qumMdX4EcLAFblelmPsGiNsnGabc148+TgYZI"
              + "1fBucn5Xrk4fxVCuqa8QjOa37aHHT5Li/xGIDCbtCqPPIi7M7O1yq8gXLWP9"
              + "TV7nsu99t4EiZT4zov9rCS+tgvBiFrRqsHL37PGrS27s+gMw3GR7F6BiDiqa"
              + "0GvLdt0Lx24c"
            );

        private static readonly byte[] attrLongLength = Convert.FromBase64String(
                "mQENBEGz0vIBCADLb2Sb5QbOhRIzfOg3u9F338gK1XZWJG8JwXP8DSGbQEof"
              + "0+YoT/7bA+3h1ljh3LG0m8JUEdolrxLz/8Mguu2TA2UQiMwRaRChSVvBgkCR"
              + "Ykr97+kClNgmi+PLuUN1z4tspqdE761nRVvUl2x4XvLTJ21hU5eXGGsC+qFP"
              + "4Efe8B5kH+FexAfnFPPzou3GjbDbYv4CYi0pyhTxmauxyJyQrQ/MQUt0RFRk"
              + "L8qCzWCR2BmH3jM3M0Wt0oKn8C8+fWItUh5U9fzv/K9GeO/SV8+zdL4MrdqD"
              + "stgqXNs27H+WeIgbXlUGIs0mONE6TtKZ5PXG5zFM1bz1vDdAYbY4eUWDABEB"
              + "AAGJAhwEHwEIAAYFAlLd55oACgkQ5ppjUk3RnxANSBAAqzYF/9hu7x7wtmi7"
              + "ScmIal6bXP14ZJaRVibMnAPEPIHAULPVa8x9QX/fGW8px5tK9YU41wigLXe6"
              + "3eC5MOLc+wkouELsBeeA3zap51/5HhsuHq5AYtL2tigce9epYUVNV9LaZd2U"
              + "vQOQ6RqyTMhSADN9mD0kR+Nu1+ns7Ur7qAq6UI39hFIGKPoZQ61pTrVsi8N7"
              + "GxHoNwa1FAxm0Dm4XvyiJHPOYs0K4OnNWLKLCcSVOx453Zj3JnllRrCFLpIt"
              + "H27jAxcbGStxWpJvlVMSylcP/x0ATjGfp+kSv2TpU2wK0W5iUtrn30W+WZp4"
              + "+BIXL0NSi4XPksoUoM9dOTsOCPh/ntiWJBlzIdhQuxgcwymoYnaAG0ermI+R"
              + "djB0gCj0AfMDZEOW+thFKg1kEkYrUnAISNDt+VZNUtk26tJ7PDitC9EY6IA6"
              + "vbKeh47LmqpyK3gqQiIA/XuWhdUOr1Wv3H8qxumFjxQQh9sr72IbWFJ+tSNl"
              + "UtrohK7N6CoJQidkj2qFsuGLcFKypAdS7Y0s0t9uOYJLwj1c+2KG0mrA2PvW"
              + "1vng9mMN6AHIx9oRSwQc1+OV29ws2hfNB3JQnpdzBYAy8C5haUWG7E7WFg+j"
              + "pNpeREVX0S+1ibmWDVs+trSQI8hd58j91Kc2YvwE13YigC9nlU2R853Gsox4"
              + "oazn75iJAhwEHwEIAAYFAlMkBMIACgkQcssEwQwvQ5L2yxAAmND9w3OZsJpF"
              + "tTAJFpfg8Bacy0Xs/+LipA1hB8vG+mvaiedcqc5KTpuFQ4bffH1swMRjXAM7"
              + "ZP/u/6qX2LL9kjxCtwDUjDT8YcphTnRxSu5Jv3w4Rf0zWIRWHhnbswiBuGwE"
              + "zQN8V20AYxfZ+ffkR0wymm/y8qLQ1oNynweijXHSlaG/sVmvDxkuc77n4hLi"
              + "4UVQiSAP7dRIkcOh6QCBW4TxoZkDfxIhASFQWl1paCagO1rwyo7YY42O4c16"
              + "+UZBMZtWTvRO2rThz1g9SxAyx8FZ7SxMv140C7VGQmdag97dA1WgBOCuLzLi"
              + "cYT+o/bL9vpFXSI7LVflQEqauzL4fs2X8ggckoI4lkjcDe8DhiDmCoju5Lat"
              + "Q/7DqV8T6z/Gv0sK2hqKr4ULC3By4N11WDCg6wXa72tMQoFBT1vOC+UzLHOj"
              + "vgWBJKE7q3E7kFfq22D0ZX0BPTYy2mcrghMzvvOe74Dx495zlUJhtBfr8MC2"
              + "uPnjsv6PjCYAaomQcvvI0o/5k8JIFi1P0pwLM5VjfujdAuCpAwQuy9AeGlz2"
              + "TEuZZlWBZuyBqZ7JyHx5xz1aVXbY7kofqO+njyyZ+MakZRLYpBI+B/8KomQP"
              + "pqWVARw4uPAXVTd1fjW2CTQtt7Ia6BRWMSblxTv3VWosTSgPnCXmzYEpGvCL"
              + "bIauL8UEhzS0JVBHUCBHbG9iYWwgRGlyZWN0b3J5IFZlcmlmaWNhdGlvbiBL"
              + "ZXmJAV4EEAECAEAFAkJRtHAHCwkIBwMCCgIZARkYbGRhcDovL2tleXNlcnZl"
              + "ci5wZ3AuY29tBRsDAAAAAxYCAQUeAQAAAAQVCAIKABIJEJcQuJvKV618B2VH"
              + "UEcAAQH35ggAnVHdAh2KqrvwSnPos73YdlVbeF9Lcbxs4oYPDCk6AHiDpjr2"
              + "nxu48i1BiLea7aTEEwwAkcIa/3lCLP02NjGXq5gRnWpW/d0xtsaDDj8yYWus"
              + "WGhEJsUlrq5Cz2KjwxNQHXRhHXEDR8vq9uzw5EjCB0u69vlwNmo8+fa17YMN"
              + "VdXaXsmXJlJciVHazdvGoscTzZOuKDHdaJmY8nJcCydk4qsFOiGOcFm5UOKP"
              + "nzdBh31NKglqw/xh+1nTA2z5orsY4jVFIB6sWqutIcVQYt/J78diAKFemkEO"
              + "Qe0kU5JZrY34E8pp4BmS6mfPyr8NtHFfMOAE4m8acFeaZK1X6+uW57QpRE5S"
              + "IEtTMSA8ZG8tbm90LXJlcGx5QGtleXNlcnZlcjEucGdwLmNvbT6JAVMEEAEC"
              + "AD0FAkmgVoIHCwkIBwMCChkYbGRhcDovL2tleXNlcnZlci5wZ3AuY29tBRsD"
              + "AAAAAxYCAQUeAQAAAAQVCAIKAAoJEJcQuJvKV618t6wH/1RFTp9Z7QUZFR5h"
              + "r8eHFWhPoeTCMXF3Vikgw2mZsjN43ZyzpxrIdUwwHROQXn1BzAvOS0rGNiDs"
              + "fOOmQFulz+Oc14xxGox2TZbdnDnXEb8ReZnimQCWYERfpRtY6GSY7uWzNjG2"
              + "dLB1y3XfsOBG+QqTULSJSZqRYD+2IpwPlAdl6qncqRvFzGcPXPIp0RS6nvoP"
              + "Jfe0u2sETDRAUDwivr7ZU/xCA12txELhcsvMQP0fy0CRNgN+pQ2b6iBL2x1l"
              + "jHgSG1r3g3gQjHEk3UCTEKHq9+mFhd/Gi0RXz6i1AmrvW4pKhbtN76WrXeF+"
              + "FXTsB09f1xKnWi4c303Ms1tIJQC0KUROUi1LUzIgPGRvLW5vdC1yZXBseUBr"
              + "ZXlzZXJ2ZXIyLnBncC5jb20+iQFTBBABAgA9BQJJoFabBwsJCAcDAgoZGGxk"
              + "YXA6Ly9rZXlzZXJ2ZXIucGdwLmNvbQUbAwAAAAMWAgEFHgEAAAAEFQgCCgAK"
              + "CRCXELibyletfBwzB/41/OkBDVLgEYnGJ78rKHLtgMdRfrL8gmZn9KhMi44H"
              + "nlFl1NAgi1yuWA2wC8DziVKIiu8YCaCVP0FFXuBK1BF8uZDRp8lZuT3Isf0/"
              + "4DX4yuvZwY5nmtDu3qXrjZ7bZi1W2A8c9Hgc+5A30R9PtiYy5Lz2m8xZl4P6"
              + "wDrYCQA2RLfzGC887bIPBK/tvXTRUFZfj2X1o/q4pr8z4NJTaFUl/XrseGcJ"
              + "R2PP3S2/fU5LErqLJhlj690xofRkf9oYrUiyyb1/UbWmNJsOHSHyy8FEc9lv"
              + "lSJIa39niSQKK6I0Mh1LheXNL7aG152KkXiH0mi6bH4EOzaTR7dfLey3o9Ph"
              + "0cye/wAADVkBEAABAQAAAAAAAAAAAAAAAP/Y/+AAEEpGSUYAAQEAAAEAAQAA"
              + "/9sAQwAKBwcIBwYKCAgICwoKCw4YEA4NDQ4dFRYRGCMfJSQiHyIhJis3LyYp"
              + "NCkhIjBBMTQ5Oz4+PiUuRElDPEg3PT47/9sAQwEKCwsODQ4cEBAcOygiKDs7"
              + "Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7"
              + "Ozs7/8AAEQgAkAB4AwEiAAIRAQMRAf/EAB8AAAEFAQEBAQEBAAAAAAAAAAAB"
              + "AgMEBQYHCAkKC//EALUQAAIBAwMCBAMFBQQEAAABfQECAwAEEQUSITFBBhNR"
              + "YQcicRQygZGhCCNCscEVUtHwJDNicoIJChYXGBkaJSYnKCkqNDU2Nzg5OkNE"
              + "RUZHSElKU1RVVldYWVpjZGVmZ2hpanN0dXZ3eHl6g4SFhoeIiYqSk5SVlpeY"
              + "mZqio6Slpqeoqaqys7S1tre4ubrCw8TFxsfIycrS09TV1tfY2drh4uPk5ebn"
              + "6Onq8fLz9PX29/j5+v/EAB8BAAMBAQEBAQEBAQEAAAAAAAABAgMEBQYHCAkK"
              + "C//EALURAAIBAgQEAwQHBQQEAAECdwABAgMRBAUhMQYSQVEHYXETIjKBCBRC"
              + "kaGxwQkjM1LwFWJy0QoWJDThJfEXGBkaJicoKSo1Njc4OTpDREVGR0hJSlNU"
              + "VVZXWFlaY2RlZmdoaWpzdHV2d3h5eoKDhIWGh4iJipKTlJWWl5iZmqKjpKWm"
              + "p6ipqrKztLW2t7i5usLDxMXGx8jJytLT1NXW19jZ2uLj5OXm5+jp6vLz9PX2"
              + "9/j5+v/aAAwDAQACEQMRAD8A9moqtf30Gm2cl3cvtijGSa4a++LNlGStlZvK"
              + "R0ZuBWkKU6nwomU4x3PQqK8guPinrEzYhhihX86ns/Ffia/XzElJUHOV4/rW"
              + "/wBUqJXlZEe2i9j1iivMP+Ex1q3+/KCw6gip4PiXdREC5tUkHcrwaTwlVbK4"
              + "e1iekUVzmheNdO1ycWyK8U5Gdrf410QOa55RcXaSNE09ULRRRUjCiiigAooo"
              + "oAKKKKAOY+IblfCN1g9cA/rXh1fQPiXT4dU0o2dwXEcrclCARgE8ZB9K4J/A"
              + "GkKeJr38ZU/+Ir0MLiIUoNSOerTlJ3R54v3hXpfg3UdNGmrHPMsToOc9+KrQ"
              + "eBdAd2SS7vkYdPnX/wCIqy3gfRoThb+9GP8AaQ/+yVdavRqxs2yYU5wdzH16"
              + "4t57+V7XHlZOCOh5rn5n5NdrJ4U0xBt/tC8x16p/8RTP+EK0uRQ32q9IPfzE"
              + "/wDiKuGKpRSSYnSm3c5/wjP5XiKFywUDqScelevR6/pCR4k1S0DDqPOXI/Wv"
              + "PLjwdplpbtPG9zI6so2yspU5YDoFHrW7pOmRWpEiqVyuPlHH41xYmPgpPmib"
              + "U4uKszqY9f0aZtseq2bN6eeuf51fVldQyMGU9CDkGueMCOpxtYe3NYWoabJJ"
              + "OZrWV7V1yFe1cxnH1HX8a57Glz0CiuFg8U6rpjql2PtkXTMgCv8Agw4/MfjX"
              + "U6VrthrCH7NKRIoy8LjDr+Hp7jIosFzRooopDCiiigClqXKRD1c/+gtWPLFi"
              + "tnUfuRH/AG//AGUiqDKGFAzAmFzG7rGhAJJyB604XtzGGjeAuD3GR2x260t1"
              + "fTJf3EChAsLKo+XOcorZP/fVQm8lPXZ/3yKLCJDPIBsjUjIHUewFWoYWS2jD"
              + "DBArPN1IQR8o/wCAirdvcERwu33ZYkdgOgLKCcfnRYBL0f8AEvmz6x/+jUqx"
              + "bzyCLCKoC92NRaiMWLkHhmj/AB+dTWlarutdoIXI64oQETXJ25MbA9DsolCE"
              + "Y4zjpVswL5QXgMB1xWZMRDIywoJn6HnAWmIzb+GZyyIisD0Vl4Nc5I0ulXSO"
              + "8zQtnMTrkGM/71dVNpufnMkm7Odwfmqd5CGi8tuQB0b5v51SEzf8M+Kl1Qix"
              + "vdqXoHysOFmA7j0PqPxHoOlrxm5DROrRkxvGQVZOCpHQivSPCfiEa9px80gX"
              + "lvhZ1Hf0Yex/mDRKNtQTN6iiioKKmoD9zGfSVf1OP61QrUuovOgZM4PBB9CD"
              + "kH865PxJrVx4d057yS0inAcIqq5TJJ+hoAqXg/4m9/8A9dU/9FR1CRUGlan/"
              + "AG7Fcal9n+z+dNjy9+/btRV64GemelWiKoRHVuIf6Ha/9e0X/oC1VIrIt/Ft"
              + "xNGsFtoxk+zoITI1zhWKjbn7vt0zSYzfvJSLAIennIB+p/pWtZy4hXmuQa71"
              + "fUzGhtre1jR920MXLHGMk+2T6da1oZb22ULM6FDwGCkHNFhGzNqCbjAmXkPG"
              + "F7VJFAkEQHBNQWkMUcQIwc859fepJJeOtNIVyK4bg1jXjda0LiTg1k3b9atE"
              + "sxr3qai0LWDoOvQXpYiEny5x6oep/Dg/hT7s9ayLoZVs1VriPeQcjIorC8F3"
              + "7ah4Vs3kbdLCvkyexXjn3xg/jRWBqb1ee/FqYLpun24P+snLMPoOK9Crzb4u"
              + "KQumSfwl2H44qo7iexB4JQHRwCMj7Q39K2roRRXTkqPLU8iuB8NFl8S6ftdg"
              + "rSHIycH5T2rvb8b2uap6MS1RDJcWsq7YUCt65J4rA0FUCHKjh2/9CNYfjDUS"
              + "kS2lskrlHDTSR/8ALPjocUaH4msUtVjCM0qLyqkAH8TyKSBnoELoOgFJf3Vo"
              + "ITFcTBNy546gevtzXM6Rqd3fakWadyigsYw3y+gAH410O/PDZHHcU7E3LWnX"
              + "ED2SC2nE0ajG4HJ/GpJJeOtYlxYpJdxXMcssLxkE+SwXdj14qrf6jrP22SK0"
              + "t4RFkFZZMYx/n8aANieXg1mXMnWla5lKRCSMFmB8xoz8qHHvzg1TnlzVIRTu"
              + "W61l3MyQRSTuNwjXdt9T2FXZ3zWfcRpPG8Mn3JBtJ9PQ/nVCO7+Dl49z4f1B"
              + "JG3Mt6XJ/wB5V/woqD4LwvDpurK45W5VT9QtFYPc1Wx6VXDfFi0M3hmG6A5t"
              + "rhSfoRj/AAruaz9d01dY0O809v8AlvEVX2bt+uKFowZ4z4Zbd4h04/8ATRv/"
              + "AEBq7+T53ufrXnXhffF4ls4JQVkildWB7EKwNehwnfLcD/aFXLcUThGs5bDU"
              + "pYrgFWZ2dGHR1J6ip57C0voRHcQq6htwI+Ug4xkEVo+MJ0jksrYA+ZuMhPou"
              + "MfzP6VnQyEqKqOqJejMmfSr/AE8NNbzC6hjG7aQVlA/kcVueFtR+12Mrpcea"
              + "gk4Abdt4/rUiMeOeaqS6UhuVubSaWymxtdrbC+YvoR6+9FhHRPcCNGaRgiqN"
              + "zFjgAVmya/pYkZftSnH8QQlT9D3rmdbefT4o7KO6ne3ky+yV9xBB9euO+Kw2"
              + "mfruNAj0OW8t/K837TB5eM7/ADBjFVp3IAOQQwyCDkEexrz95W9vrirula1L"
              + "YyiOQu9s2Q0YPT3GehpgdJK2apzt8hottQgv1k8pZEeMZIYg5GcZyKjuFkkK"
              + "QxKXklYKijqSeAKdwPUvhdbeX4ZmutpH2y7eUZ9AAv8ANTRXSaJpqaPotnpy"
              + "YP2eIKxHdv4j+JyaKwe5qi/RRRSGeaeJ/Dx03x7Yavbr/o967eZj+GQI38xz"
              + "+dXdPffczD1cVu+Lzi0tT6Tj/wBBNc3oz7r5x6uKroIwPFt5BeazFbQKGa1B"
              + "WSQdycfL+GP1qCCPgU3+yprC/ltrpcSqxOezAnhge9aMNv04rRaIh7jEiNSS"
              + "FLeF55c7I1LNjrgVcjt/alu9O+12U1uSUEqFNyjlcjrRcVjzzVL6bU5xJIio"
              + "qjCIo4Uf1NUDEfStiXTLizuHtboL5qc7l6OvZhTTZ+1K4WMZoSe1NFuSelbP"
              + "2M9xT47As2FXJp3FYqaUptJ2fZu3IVwSR1r0L4f6FHqmsf2w8bC3sjhA2CGl"
              + "x29duc/UisHQ/DlzreoiwtPl24NxPjKwL/Vj2H9K9m07T7bStPhsbOPy4IV2"
              + "qO/uT6knkmoky4otUUUVBYUUUUAc54yP+hWv/XwB+hrntOTyNbSP+84rs9Z0"
              + "4ajaqu7a8bh0OMjI9a5O6gvob3zjZAuDwyOMfryKaegEHjZTYva6qV8yFf3M"
              + "qKMsueQw9uDmq+nPZahGJLSdHz2zyKsXEOpagyC4IWOM5WNOmfUnuaxtT8NO"
              + "JPtFoGt5uu6PjP4U0xNHSx2bjtmrC2p/u1xEOr+J9MO1sXCj++OavxeO9Tj4"
              + "m0vJ9jTuI09c8NrqUavGfKuI/wDVyhc49iO4rnToV/A/lXCI5xkPGCFI/HvW"
              + "hL491BhiLSufc1l6hrXiTVZQIALaPGOFyfc0gHzadBZxGW9nSFBydxp+nafP"
              + "rEii0RrOyP3rmRfncf7Cn+Z/Wo9K8NXEl0Lm+L3EgOQZTux9K7W0s5BgYNFw"
              + "sbOg2tlpVilnYxCOMHJ7s7HqxPc1sqcjNZNnbsuM1qoMLUlD6KKKACiiigBC"
              + "M1E9tG55UVNRQBWNlF2UVC+mxP8Aw1fooAx5NDgfqg/KoG8N2p/5ZL+Vb9FA"
              + "HPjw1ag/6pfyqZNBt06IPyraooAzU0qJOiirCWcadBVqigBixhegp1LRQAUU"
              + "UUAf/9mJAVYEEAECADgFAkJRtHAHCwkIBwMCChkYbGRhcDovL2tleXNlcnZl"
              + "ci5wZ3AuY29tBRsDAAAAAxYCAQUeAQAAAAASCRCXELibyletfAdlR1BHAAEB"
              + "SBIH/j+RGcMuHmVoZq4+XbmCunnbft4T0Ta4o6mxNkc6wk5P9PpcE9ixztjV"
              + "ysMmv2i4Y746dCY9B1tfhQW10S39HzrYHh3I4a2wb9zQniZCf1XnbCe1eRss"
              + "NhTpLVXXnXKEsc9EwD5MtiPICluZIXB08Zx2uJSZ+/i9TqSM5EUuJk+lXqgX"
              + "GUiTaSXN63I/4BnbFzCw8SaST7d7nok45UC9I/+gcKVO+oYETgrsU7AL6uk1"
              + "6YD9JpfYZHEFmpYoS+qQ3tLfPCG3gaS/djBZWWkNt5z7e6sbRko49XEj3EUh"
              + "33HgjrOlL8uJNbhlZ5NeILcxHqGTHji+5wMEDBjfNT/C6m0=");

        [Test]
        public void DoTestRemoveSignature()
        {
            byte[] testPubKeyRing =
                Convert.FromBase64String(
                    "mQGiBEAR8jYRBADNifuSopd20JOQ5x30ljIaY0M6927+vo09NeNxS3KqItba"
                        + "nz9o5e2aqdT0W1xgdHYZmdElOHTTsugZxdXTEhghyxoo3KhVcNnTABQyrrvX"
                        + "qouvmP2fEDEw0Vpyk+90BpyY9YlgeX/dEA8OfooRLCJde/iDTl7r9FT+mts8"
                        + "g3azjwCgx+pOLD9LPBF5E4FhUOdXISJ0f4EEAKXSOi9nZzajpdhe8W2ZL9gc"
                        + "BpzZi6AcrRZBHOEMqd69gtUxA4eD8xycUQ42yH89imEcwLz8XdJ98uHUxGJi"
                        + "qp6hq4oakmw8GQfiL7yQIFgaM0dOAI9Afe3m84cEYZsoAFYpB4/s9pVMpPRH"
                        + "NsVspU0qd3NHnSZ0QXs8L8DXGO1uBACjDUj+8GsfDCIP2QF3JC+nPUNa0Y5t"
                        + "wKPKl+T8hX/0FBD7fnNeC6c9j5Ir/Fp/QtdaDAOoBKiyNLh1JaB1NY6US5zc"
                        + "qFks2seZPjXEiE6OIDXYra494mjNKGUobA4hqT2peKWXt/uBcuL1mjKOy8Qf"
                        + "JxgEd0MOcGJO+1PFFZWGzLQ3RXJpYyBILiBFY2hpZG5hICh0ZXN0IGtleSBv"
                        + "bmx5KSA8ZXJpY0Bib3VuY3ljYXN0bGUub3JnPohZBBMRAgAZBQJAEfI2BAsH"
                        + "AwIDFQIDAxYCAQIeAQIXgAAKCRAOtk6iUOgnkDdnAKC/CfLWikSBdbngY6OK"
                        + "5UN3+o7q1ACcDRqjT3yjBU3WmRUNlxBg3tSuljmwAgAAuQENBEAR8jgQBAC2"
                        + "kr57iuOaV7Ga1xcU14MNbKcA0PVembRCjcVjei/3yVfT/fuCVtGHOmYLEBqH"
                        + "bn5aaJ0P/6vMbLCHKuN61NZlts+LEctfwoya43RtcubqMc7eKw4k0JnnoYgB"
                        + "ocLXOtloCb7jfubOsnfORvrUkK0+Ne6anRhFBYfaBmGU75cQgwADBQP/XxR2"
                        + "qGHiwn+0YiMioRDRiIAxp6UiC/JQIri2AKSqAi0zeAMdrRsBN7kyzYVVpWwN"
                        + "5u13gPdQ2HnJ7d4wLWAuizUdKIQxBG8VoCxkbipnwh2RR4xCXFDhJrJFQUm+"
                        + "4nKx9JvAmZTBIlI5Wsi5qxst/9p5MgP3flXsNi1tRbTmRhqIRgQYEQIABgUC"
                        + "QBHyOAAKCRAOtk6iUOgnkBStAJoCZBVM61B1LG2xip294MZecMtCwQCbBbsk"
                        + "JVCXP0/Szm05GB+WN+MOCT2wAgAA");

            PgpPublicKeyRing pgpPub = new PgpPublicKeyRing(testPubKeyRing);

            var pubKey = pgpPub.GetPublicKey();
            Assert.AreEqual(0, pubKey.KeyCertifications.Count);

            var firstUserId = pubKey.GetUserIds().FirstOrDefault();
            Assert.NotNull(firstUserId);
            Assert.AreEqual(1, firstUserId.SelfCertifications.Count);
            Assert.AreEqual(0, firstUserId.OtherCertifications.Count);
            Assert.AreEqual(0, firstUserId.RevocationSignatures.Count);

            Assert.AreEqual(PgpSignatureType.PositiveCertification, firstUserId.SelfCertifications[0].Signature.SignatureType);

            var newPubKey = PgpPublicKey.RemoveCertification(pubKey, firstUserId, firstUserId.SelfCertifications[0]);
            firstUserId = newPubKey.GetUserIds().FirstOrDefault();
            Assert.NotNull(firstUserId);
            Assert.AreEqual(0, firstUserId.SelfCertifications.Count);
            Assert.AreEqual(0, firstUserId.OtherCertifications.Count);
            Assert.AreEqual(0, firstUserId.RevocationSignatures.Count);
        }

        [Test]
        public void DSASmallSignatureRegression()
        {
            var pgpPriv = new PgpSecretKeyRing(dsaKeyRing);
            var secretKey = pgpPriv.GetSecretKey();
            var m = Convert.FromBase64String("kA0DAQIRzSP16cTKNEMBrEZ0CF9DT05TT0xFX+uYQWhlbGxvIHdvcmxkIQ0KaGVsbG8gd29ybGQhDQpoZWxsbyB3b3JsZCENCmhlbGxvIHdvcmxkIQ0KiD0DBQFf65hBzSP16cTKNEMRAvGjAJd00mbcvtYoZHENbtlbb+qmcg5jAJdR7jNL8HIA+LcuB5aUIT2n8pNp");
            verifySignature(m, PgpHashAlgorithm.Sha1, secretKey, TEST_DATA_WITH_CRLF, checkTime: false);
        }

        [Test]
        public void PerformTest()
        {
            //
            // RSA tests
            //
            PgpSecretKeyRing pgpPriv = new PgpSecretKeyRing(rsaKeyRing);
            PgpSecretKey secretKey = pgpPriv.GetSecretKey();
            PgpPrivateKey pgpPrivKey = secretKey.ExtractPrivateKey(rsaPass);

            //
            // certifications
            //
            var revocation = PgpCertification.GenerateKeyRevocation(secretKey, pgpPrivKey, secretKey);

            Assert.IsTrue(revocation.Verify(secretKey));
            Assert.IsTrue(revocation.Verify());

            PgpSecretKeyRing pgpDSAPriv = new PgpSecretKeyRing(dsaKeyRing);
            PgpSecretKey secretDSAKey = pgpDSAPriv.GetSecretKey();
            PgpPrivateKey pgpPrivDSAKey = secretDSAKey.ExtractPrivateKey(dsaPass);

            var hashedAttributes = new PgpSignatureAttributes();
            hashedAttributes.SetSignatureExpirationTime(false, TEST_EXPIRATION_TIME);
            hashedAttributes.SetSignerUserId(true, TEST_USER_ID);
            hashedAttributes.SetPreferredCompressionAlgorithms(false, PREFERRED_COMPRESSION_ALGORITHMS);
            hashedAttributes.SetPreferredHashAlgorithms(false, PREFERRED_HASH_ALGORITHMS);
            hashedAttributes.SetPreferredSymmetricAlgorithms(false, PREFERRED_SYMMETRIC_ALGORITHMS);

            var subkeyBinding = PgpCertification.GenerateSubkeyBinding(
                secretDSAKey,
                pgpPrivDSAKey,
                secretKey,
                hashedAttributes);

            Assert.IsTrue(subkeyBinding.Verify(secretDSAKey));

            PgpSignatureAttributes hashedPcks = subkeyBinding.HashedAttributes;
            PgpSignatureAttributes unhashedPcks = subkeyBinding.UnhashedAttributes;

            Assert.AreEqual(TEST_USER_ID, hashedPcks.SignerUserId);
            Assert.AreEqual(TEST_EXPIRATION_TIME, hashedPcks.SignatureExpirationTime);
            Assert.AreEqual(secretDSAKey.KeyId, unhashedPcks.IssuerKeyId);

            preferredAlgorithmCheck("compression", PREFERRED_COMPRESSION_ALGORITHMS, hashedPcks.PreferredCompressionAlgorithms);
            preferredAlgorithmCheck("hash", PREFERRED_HASH_ALGORITHMS, hashedPcks.PreferredHashAlgorithms);
            preferredAlgorithmCheck("symmetric", PREFERRED_SYMMETRIC_ALGORITHMS, hashedPcks.PreferredSymmetricAlgorithms);

            /*SignatureSubpacketTag[] criticalHashed = hashedPcks.GetCriticalTags();

            if (criticalHashed.Length != 1)
            {
                Fail("wrong number of critical packets found.");
            }

            if (criticalHashed[0] != SignatureSubpacketTag.SignerUserId)
            {
                Fail("wrong critical packet found in tag list.");
            }*/

            //
            // no packets passed
            //

            subkeyBinding = PgpCertification.GenerateSubkeyBinding(
                secretDSAKey,
                pgpPrivDSAKey,
                secretKey);

            Assert.IsTrue(subkeyBinding.Verify(secretDSAKey));

            hashedPcks = subkeyBinding.HashedAttributes;
            Assert.IsTrue(hashedPcks.SignatureCreationTime.HasValue);

            unhashedPcks = subkeyBinding.UnhashedAttributes;
            Assert.IsTrue(unhashedPcks.IssuerKeyId.HasValue);

            /*try
            {
                sig.VerifyRevocation(secretKey.PublicKey);

                Fail("failed to detect non-key signature.");
            }
            catch (InvalidOperationException)
            {
                // expected
            }*/

            //
            // override hash packets
            //
            DateTime creationTime = new DateTime(1973, 7, 27);

            hashedAttributes = new PgpSignatureAttributes();
            hashedAttributes.SetSignatureCreationTime(false, creationTime);

            subkeyBinding = PgpCertification.GenerateSubkeyBinding(
                secretDSAKey,
                pgpPrivDSAKey,
                secretKey,
                hashedAttributes);

            Assert.IsTrue(subkeyBinding.Verify(secretDSAKey));

            hashedPcks = subkeyBinding.HashedAttributes;

            Assert.IsTrue(hashedPcks.SignatureCreationTime.HasValue);
            Assert.AreEqual(creationTime, hashedPcks.SignatureCreationTime);

            preferredAlgorithmCheck("compression", null, hashedPcks.PreferredCompressionAlgorithms);
            preferredAlgorithmCheck("hash", null, hashedPcks.PreferredHashAlgorithms);
            preferredAlgorithmCheck("symmetric", null, hashedPcks.PreferredSymmetricAlgorithms);

            Assert.AreEqual(null, hashedPcks.KeyExpirationTime);
            Assert.AreEqual(null, hashedPcks.SignatureExpirationTime);
            Assert.AreEqual(null, hashedPcks.SignerUserId);

            unhashedPcks = subkeyBinding.UnhashedAttributes;

            Assert.IsTrue(unhashedPcks.IssuerKeyId.HasValue);

            //
            // general signatures
            //
            doTestSig(PgpHashAlgorithm.Sha256, secretKey, pgpPrivKey);
            doTestSig(PgpHashAlgorithm.Sha384, secretKey, pgpPrivKey);
            doTestSig(PgpHashAlgorithm.Sha512, secretKey, pgpPrivKey);
            doTestSig(PgpHashAlgorithm.Sha1, secretKey, pgpPrivKey, version: 3);
            doTestTextSig(PgpHashAlgorithm.Sha1, secretKey, pgpPrivKey, TEST_DATA_WITH_CRLF, TEST_DATA_WITH_CRLF);
            doTestTextSig(PgpHashAlgorithm.Sha1, secretKey, pgpPrivKey, TEST_DATA, TEST_DATA_WITH_CRLF);
            doTestTextSig(PgpHashAlgorithm.Sha1, secretKey, pgpPrivKey, TEST_DATA_WITH_CRLF, TEST_DATA_WITH_CRLF, version: 3);
            doTestTextSig(PgpHashAlgorithm.Sha1, secretKey, pgpPrivKey, TEST_DATA, TEST_DATA_WITH_CRLF, version: 3);

            //
            // DSA Tests
            //
            pgpPriv = new PgpSecretKeyRing(dsaKeyRing);
            secretKey = pgpPriv.GetSecretKey();
            pgpPrivKey = secretKey.ExtractPrivateKey(dsaPass);


            doTestSig(PgpHashAlgorithm.Sha1, secretKey, pgpPrivKey);
            doTestSig(PgpHashAlgorithm.Sha1, secretKey, pgpPrivKey, version: 3);
            doTestTextSig(PgpHashAlgorithm.Sha1, secretKey, pgpPrivKey, TEST_DATA_WITH_CRLF, TEST_DATA_WITH_CRLF);
            doTestTextSig(PgpHashAlgorithm.Sha1, secretKey, pgpPrivKey, TEST_DATA, TEST_DATA_WITH_CRLF);
            doTestTextSig(PgpHashAlgorithm.Sha1, secretKey, pgpPrivKey, TEST_DATA_WITH_CRLF, TEST_DATA_WITH_CRLF, version: 3);
            doTestTextSig(PgpHashAlgorithm.Sha1, secretKey, pgpPrivKey, TEST_DATA, TEST_DATA_WITH_CRLF, version: 3);

            // special cases
            //
            doTestMissingSubpackets(nullPacketsSubKeyBinding);

            //doTestMissingSubpackets(generateV3BinarySig(pgpPrivKey, HashAlgorithmTag.Sha1));

            // keyflags
            //doTestKeyFlagsValues();

            // TODO Seems to depend on some other functionality that's yet to be ported
            //doTestUserAttributeEncoding();
        }

        //private void doTestUserAttributeEncoding()
        //{
        //    PgpPublicKeyRing pkr = new PgpPublicKeyRing(okAttr);

        //    CheckUserAttribute("normal", pkr, pkr.GetPublicKey());

        //    pkr = new PgpPublicKeyRing(attrLongLength);

        //    CheckUserAttribute("long", pkr, pkr.GetPublicKey());
        //}

        //private void CheckUserAttribute(String type, PgpPublicKeyRing pkr, PgpPublicKey masterPk)
        //{
        //    foreach (PgpUserAttributeSubpacketVector attr in pkr.GetPublicKey().GetUserAttributes())
        //    {
        //        foreach (PgpSignature sig in masterPk.GetSignaturesForUserAttribute(attr))
        //        {
        //            sig.InitVerify(masterPk);
        //            if (!sig.VerifyCertification(attr, masterPk))
        //            {
        //                Fail("user attribute sig failed to verify on " + type);
        //            }
        //        }
        //    }
        //}

        /*private void doTestKeyFlagsValues()
        {
            checkValue(KeyFlags.CertifyOther, 0x01);
            checkValue(KeyFlags.SignData, 0x02);
            checkValue(KeyFlags.EncryptCommunications, 0x04);
            checkValue(KeyFlags.EncryptStorage, 0x08);
            checkValue(KeyFlags.Split, 0x10);
            checkValue(KeyFlags.Authentication, 0x20);
            checkValue(KeyFlags.Shared, 0x80);

            // yes this actually happens
            checkValue(new byte[] { 4, 0, 0, 0 }, 0x04);
            checkValue(new byte[] { 4, 0, 0 }, 0x04);
            checkValue(new byte[] { 4, 0 }, 0x04);
            checkValue(new byte[] { 4 }, 0x04);
        }

        private void checkValue(KeyFlags flag, int val)
        {
            Sig.KeyFlags f = new Sig.KeyFlags(true, flag);
            if ((int)f.Flags != val)
            {
                Fail("flag value mismatch");
            }
        }

        private void checkValue(byte[] flag, int val)
        {
            Sig.KeyFlags f = new Sig.KeyFlags(true, false, flag);
            if ((int)f.Flags != val)
            {
                Fail("flag value mismatch");
            }
        }*/

        private void doTestMissingSubpackets(byte[] signature)
        {
            PgpSignature sig = new PgpSignature(signature);

            if (sig.Version > 3)
            {
                PgpSignatureAttributes v = sig.HashedAttributes;
                Assert.IsFalse(v.KeyExpirationTime.HasValue);
            }
            else
            {
                /*if (sig.GetHashedSubPackets() != null)
                {
                    Fail("hashed sub packets found when none expected");
                }

                if (sig.GetUnhashedSubPackets() != null)
                {
                    Fail("unhashed sub packets found when none expected");
                }

                if (sig.HasSubpackets)
                {
                    Fail("HasSubpackets property was true with no packets");
                }*/
            }
        }

        private void preferredAlgorithmCheck<T>(
            string type,
            T[] expected,
            T[] prefAlgs)
            where T : Enum
        {
            if (expected == null)
            {
                if (prefAlgs != null)
                {
                    Fail("preferences for " + type + " found when none expected");
                }
            }
            else
            {
                if (prefAlgs.Length != expected.Length)
                {
                    Fail("wrong number of preferred " + type + " algorithms found");
                }

                for (int i = 0; i != expected.Length; i++)
                {
                    if (!Equals(expected[i], prefAlgs[i]))
                    {
                        Fail("wrong algorithm found for " + type + ": expected " + expected[i] + " got " + prefAlgs);
                    }
                }
            }
        }

        private void doTestSig(
            PgpHashAlgorithm hashAlgorithm,
            PgpKey pubKey,
            PgpPrivateKey privKey,
            int version = 4)
        {
            MemoryStream bOut = new MemoryStream();

            var messageGenerator = new PgpMessageGenerator(bOut);
            using (var signingGenerator = messageGenerator.CreateSigned(PgpSignatureType.BinaryDocument, privKey, hashAlgorithm, version))
            using (var literalStream = signingGenerator.CreateLiteral(PgpDataFormat.Binary, "_CONSOLE", DateTime.UtcNow))
            {
                literalStream.Write(TEST_DATA);
                literalStream.Write(TEST_DATA);
            }

            verifySignature(bOut.ToArray(), hashAlgorithm, pubKey, TEST_DATA);
        }

        private void doTestTextSig(
            PgpHashAlgorithm hashAlgorithm,
            PgpKey pubKey,
            PgpPrivateKey privKey,
            byte[] data,
            byte[] canonicalData,
            int version = 4)
        {
            MemoryStream bOut = new MemoryStream();
            var messageGenerator = new PgpMessageGenerator(bOut);
            using (var signingGenerator = messageGenerator.CreateSigned(PgpSignatureType.CanonicalTextDocument, privKey, PgpHashAlgorithm.Sha1, version))
            using (var literalStream = signingGenerator.CreateLiteral(PgpDataFormat.Text, "_CONSOLE", DateTime.UtcNow))
            {
                literalStream.Write(data);
                literalStream.Write(canonicalData);
            }

            /*if (sig.CreationTime == DateTimeOffset.FromUnixTimeSeconds(0).DateTime)
            {
                Fail("creation time not set in v4 signature");
            }*/

            verifySignature(bOut.ToArray(), hashAlgorithm, pubKey, canonicalData);
        }

        private void verifySignature(
            byte[] encodedSig,
            PgpHashAlgorithm hashAlgorithm,
            PgpKey pubKey,
            byte[] original,
            bool checkTime = false)
        {
            var now = DateTime.UtcNow;
            var signedMessage = (PgpSignedMessage)PgpMessage.ReadMessage(encodedSig);
            var literalMessage = (PgpLiteralMessage)signedMessage.ReadMessage();
            literalMessage.GetStream().CopyTo(Stream.Null);
            Assert.IsTrue(signedMessage.Verify(pubKey, out DateTime creationTime));
            Assert.IsTrue(!checkTime || Math.Abs((creationTime - now).TotalMinutes) < 10);
            Assert.AreEqual(pubKey.KeyId, signedMessage.KeyId);
            /*
            sig.InitVerify(pubKey);

            sig.Update(original);
            sig.Update(original);

            if (!sig.Verify())
            {
                Fail("Failed generated signature check against original data");
            }*/
        }
    }
}
