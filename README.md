<p align="center">
  <picture>
    <source media="(prefers-color-scheme: dark)" srcset="https://github.com/BrantaOps/Assets/blob/main/svg/logo-white.svg?raw=true">
    <source media="(prefers-color-scheme: light)" srcset="https://github.com/BrantaOps/Assets/blob/main/svg/logo-black.svg?raw=true">
    <img alt="Branta" src="Branta/Assets/goldblackcropped.jpg" width="500">
  </picture>
</p>

# Branta BTCPay Plugin

A template for your own [BTCPay Server](https://github.com/btcpayserver) plugin.

Learn more in our [plugin documentation](https://docs.btcpayserver.org/Development/Plugins/).

## Local Development

### Dependencies

 - [.NET 8](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
 - [docker](https://www.docker.com/products/docker-desktop/)

### Project Setup
 - Create root folder (Ex: branta-btcpayserver)
 - Clone https://github.com/btcpayserver/btcpayserver in your root folder
     - Checkout v1.3.X branch
 - Clone https://github.com/BrantaOps/btcpay in your root folder
  ```sh
  git clone git@github.com:BrantaOps/btcpay.git --recurse-submodules btcpayserver-plugin-branta
  ```

Folder structure should look like
```sh
branta-btcpayserver # (root)
|_ btcpayserver
|_ btcpayserver-plugin-branta
  |_ btcpayserver
  |_ BTCPayServer.Plugins.Branta
```

### Migrations
 * Set `Branta.Migrations` project as the Startup Project (not in source control)
 * Set `BTCPayServer.Plugins.Branta` as the Default Project
 * Run migration `Add-Migration -c BrantaDbContext "<Migration-Name-Here>"`

### Install Plugin

To install the plugin you can either `Reference in Project` or `Generate .btcpay File`

#### Reference in Project

1. Reference plugin project
```sh
# Enter the forked BTCPay Server repository
cd btcpayserver

# Add your plugin to the solution
dotnet sln add ../btcpayserver-plugin-branta/BTCPayServer.Plugins.Branta -s Plugins
```

2. Create appsettings.dev.json file

Path: branta-btcpayserver/btcpayserver/BTCPayServer/appsettings.dev.json
```json
{
  "DEBUG_PLUGINS": "/absolute/path/btcpayserver-plugin-branta/BTCPay.Plugins.Branta/bin/Debug/net6.0/BTCPayServer.Plugins.Branta.dll"
}
```   

#### Generate .btcpay File

1. Follow commands in `BTCPayServer.Plugins.Branta/Makefile` for your operating system to generate .btcpay file
2. When running BTCPayServer project on the left nav click the "Manage Plugins" link
3. Scroll to the bottom you will see "Upload Plugin" option

### Run the project

1. Run docker
```sh
cd btcpayserver/BTCPayServer.Tests
docker-compose up dev
```

2. Run the application in Debug
```sh
cd btcpayserver/BTCPayServer
dotnet run -c Debug
```
