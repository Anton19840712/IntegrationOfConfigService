namespace Application.Interfaces
{
    public interface IDataEncryptor
    {
        string Encrypt(string plainText);
        string Decrypt(string cipherText);
    }
}
