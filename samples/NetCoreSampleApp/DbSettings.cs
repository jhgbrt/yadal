#pragma warning disable

namespace NetCoreSampleApp
{
    /*
     * This sample shows how the Db class can be configured with DI in a standard .Net Core
     * application that is based on the generic host.
     * 
     * The IDb service is added as a scoped service. The connection string is read from an 
     * appSettings.json file and the DbProviderFactory instance is passed in directly.
     * 
     */

    class DbSettings
    {
        public string ConnectionString { get; set; }
    }



}
