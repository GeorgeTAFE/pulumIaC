using Pulumi;
using Pulumi.AzureNative.Resources;
using Pulumi.AzureNative.Sql;
using Pulumi.AzureNative.Web;
using Pulumi.AzureNative.Storage;
using Pulumi.AzureNative.Storage.Inputs;
using System.Collections.Generic;

// Top-level statement to run the Pulumi deployment
return await Pulumi.Deployment.RunAsync<MvcAppDeployment.MyStack>();

namespace MvcAppDeployment
{
    // Define the MyStack class inside the namespace
    class MyStack : Stack
    {
        public MyStack()
        {
            // Create an Azure Resource Group
            var resourceGroup = new ResourceGroup("mvcapp-rg");

            // Create a SQL Server
            var sqlServer = new Server("sqlserver", new ServerArgs
            {
                ResourceGroupName = resourceGroup.Name,
                AdministratorLogin = "sqladmin",
                AdministratorLoginPassword = "StrongPassword123!",
                Location = resourceGroup.Location
            });

            // Create a SQL Database
            var database = new Database("appdb", new DatabaseArgs
            {
                ResourceGroupName = resourceGroup.Name,
                ServerName = sqlServer.Name
            });

            // Create a firewall rule for GitHub Actions or specific IP range
            var sqlFirewallRule = new FirewallRule("allowAllIps", new FirewallRuleArgs
            {
                ResourceGroupName = resourceGroup.Name,
                ServerName = sqlServer.Name,
                StartIpAddress = "0.0.0.0",  // Allow access from all IPs (less secure)
                EndIpAddress = "255.255.255.255"
            });

            // Create an App Service Plan
            var appServicePlan = new AppServicePlan("aspPlan", new AppServicePlanArgs
            {
                ResourceGroupName = resourceGroup.Name,
                Sku = new Pulumi.AzureNative.Web.Inputs.SkuDescriptionArgs
                {
                    Name = "B1",
                    Tier = "Basic"
                }
            });

            // Create a Web App and configure it with the SQL connection string
            var app = new WebApp("mvcapp", new WebAppArgs
            {
                ResourceGroupName = resourceGroup.Name,
                ServerFarmId = appServicePlan.Id,
                SiteConfig = new Pulumi.AzureNative.Web.Inputs.SiteConfigArgs
                {
                    ConnectionStrings = new[]
                    {
                        new Pulumi.AzureNative.Web.Inputs.ConnStringInfoArgs
                        {
                            Name = "DefaultConnection",
                            ConnectionString = Output.Format($"Server=tcp:{sqlServer.FullyQualifiedDomainName};Database={database.Name};User ID=sqladmin;Password=StrongPassword123!"),
                            Type = ConnectionStringType.SQLAzure
                        }
                    },

                    Http20Enabled = true, // Enables HTTP/2 (optional but recommended for performance)
                    AlwaysOn = true, // Keep the app always on                   

                    // CORS configuration
                    Cors = new Pulumi.AzureNative.Web.Inputs.CorsSettingsArgs
                    {
                        AllowedOrigins = { "*" }, // Modify as per your app's CORS needs
                    }
                },
                // Enforcing HTTPS by adding HTTP-to-HTTPS redirection rule
                HttpsOnly = true, // Force HTTPS connections
            });





            // Export the Web App endpoint URL
            this.Endpoint = Output.Format($"https://{app.DefaultHostName}");
        }

        // Export the web app endpoint and storage key as stack outputs
        [Output]
        public Output<string> Endpoint { get; set; }

        // [Output]
        // public Output<string> StorageAccountKey { get; set; }

        [Output]
        public Output<string>? StorageAccountKey { get; set; }

    }
}

