using Microsoft.AspNetCore.Http;
using System.Data;

namespace MP.Client.Common.ClientMetadata
{
    public interface IMetadataCreator
    {
        Metadata CreateMetadata(IDbConnection _connection, HttpRequest request);
    }
}
