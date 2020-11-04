using System;
using System.Security.Cryptography;

namespace MP.Core.Common.Auth
{
    public sealed class PasswordHasher
    {
        private const int VERSION = 1;
        private const int ITERATIONS = 1000;
        private const int NUM_BYTES_REQUESTED = 256 / 8; // 256 bits
        private const int SALT_SIZE = 128 / 8; // 128 bits
        private static readonly HashAlgorithmName HASH_ALGORITHM_NAME = HashAlgorithmName.SHA256;

        public static string HashPassword(string password)
        {
            if (password == null)
                throw new ArgumentNullException(nameof(password));

            byte[] salt;
            byte[] key;
            byte[] iterationBytes = BitConverter.GetBytes(ITERATIONS);

            using (var algorithm = new Rfc2898DeriveBytes(password, SALT_SIZE, ITERATIONS, HASH_ALGORITHM_NAME))
            {
                salt = algorithm.Salt;
                key = algorithm.GetBytes(NUM_BYTES_REQUESTED);
            }

            int byteArraySize = 1 + SALT_SIZE + NUM_BYTES_REQUESTED + iterationBytes.Length;
            int saltOffset = 1 + NUM_BYTES_REQUESTED;
            int iterationsOffset = 1 + NUM_BYTES_REQUESTED + SALT_SIZE;
            var inArray = new byte[byteArraySize];

            inArray[0] = VERSION;
            Buffer.BlockCopy(key, 0, inArray, 1, NUM_BYTES_REQUESTED);
            Buffer.BlockCopy(salt, 0, inArray, saltOffset, SALT_SIZE);
            Buffer.BlockCopy(iterationBytes, 0, inArray, iterationsOffset, iterationBytes.Length);

            return Convert.ToBase64String(inArray);
        }

        public static PasswordVerificationResult VerifyHashedPassword(string hashedPassword, string password)
        {
            if (password == null)
                throw new ArgumentNullException(nameof(password));

            if (hashedPassword == null)
                return PasswordVerificationResult.Failed;

            byte[] hashBytes = Convert.FromBase64String(hashedPassword);
            if (hashBytes.Length == 0)
                return PasswordVerificationResult.Failed;

            int version = hashBytes[0];

            byte[] sourceKey = new byte[NUM_BYTES_REQUESTED];
            Buffer.BlockCopy(hashBytes, 1, sourceKey, 0, NUM_BYTES_REQUESTED);

            int saltOffset = 1 + NUM_BYTES_REQUESTED;
            byte[] salt = new byte[SALT_SIZE];
            Buffer.BlockCopy(hashBytes, saltOffset, salt, 0, SALT_SIZE);

            int iterationsBytesSize = hashBytes.Length - NUM_BYTES_REQUESTED - SALT_SIZE - 1;
            int iterationsOffset = 1 + NUM_BYTES_REQUESTED + SALT_SIZE;
            byte[] iterationsBytes = new byte[iterationsBytesSize];
            Buffer.BlockCopy(hashBytes, iterationsOffset, iterationsBytes, 0, iterationsBytes.Length);
            int currentIterations = BitConverter.ToInt32(iterationsBytes);

            byte[] comparisonKey;
            using (var rfc2898DeriveBytes 
                = new Rfc2898DeriveBytes(password, salt, currentIterations, HASH_ALGORITHM_NAME))
            {
                comparisonKey = rfc2898DeriveBytes.GetBytes(NUM_BYTES_REQUESTED);
            }

            if (CryptographicOperations.FixedTimeEquals(sourceKey, comparisonKey))
            {
                return (version < VERSION || currentIterations != ITERATIONS)
                    ? PasswordVerificationResult.SuccessRehashNeeded
                    : PasswordVerificationResult.Success;
            }

            return PasswordVerificationResult.Failed;
        }
    }

    public enum PasswordVerificationResult
    {
        Failed,
        Success,
        SuccessRehashNeeded
    }
}
