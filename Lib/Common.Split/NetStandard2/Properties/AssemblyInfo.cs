using System;
using System.Reflection;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("Microsoft.Azure.Storage.Common.dll")]
[assembly: AssemblyDescription("Azure Storage Common SDK for Windows")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Microsoft")]
[assembly: AssemblyProduct("Windows Azure Storage Common")]
[assembly: AssemblyCopyright("Copyright © 2019 Microsoft Corp.")]
[assembly: AssemblyTrademark("Microsoft ® is a registered trademark of Microsoft Corporation.")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("a89a167c-9cc6-46b5-a50b-697b69bfe078")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Build and Revision Numbers 
// by using the '*' as shown below:
// [assembly: AssemblyVersion("1.0.*")]

[assembly: AssemblyVersion("11.2.1.0")]
[assembly: AssemblyFileVersion("11.2.1.0")]
[assembly: AssemblyInformationalVersion("11.2.1.0")]


#if SIGN
[assembly: InternalsVisibleTo(
    "Microsoft.Azure.Storage.Queue, PublicKey=" +
    "0024000004800000940000000602000000240000525341310004000001000100b5fc90e7027f67" +
    "871e773a8fde8938c81dd402ba65b9201d60593e96c492651e889cc13f1415ebb53fac1131ae0b" +
    "d333c5ee6021672d9718ea31a8aebd0da0072f25d87dba6fc90ffd598ed4da35e44c398c454307" +
    "e8e33b8426143daec9f596836f97c8f74750e5975c64e2189f45def46b2a2b1247adc3652bf5c3" +
    "08055da9")]
[assembly: InternalsVisibleTo(
    "Microsoft.Azure.Storage.File, PublicKey=" +
    "0024000004800000940000000602000000240000525341310004000001000100b5fc90e7027f67" +
    "871e773a8fde8938c81dd402ba65b9201d60593e96c492651e889cc13f1415ebb53fac1131ae0b" +
    "d333c5ee6021672d9718ea31a8aebd0da0072f25d87dba6fc90ffd598ed4da35e44c398c454307" +
    "e8e33b8426143daec9f596836f97c8f74750e5975c64e2189f45def46b2a2b1247adc3652bf5c3" +
    "08055da9")]
[assembly: InternalsVisibleTo(
    "Microsoft.Azure.Storage.Blob, PublicKey=" +
    "0024000004800000940000000602000000240000525341310004000001000100b5fc90e7027f67" +
    "871e773a8fde8938c81dd402ba65b9201d60593e96c492651e889cc13f1415ebb53fac1131ae0b" +
    "d333c5ee6021672d9718ea31a8aebd0da0072f25d87dba6fc90ffd598ed4da35e44c398c454307" +
    "e8e33b8426143daec9f596836f97c8f74750e5975c64e2189f45def46b2a2b1247adc3652bf5c3" +
    "08055da9")]
[assembly: InternalsVisibleTo("Microsoft.Azure.Storage.Test, PublicKey=" +
    "0024000004800000940000000602000000240000525341310004000001000100b5fc90e7027f67" +
    "871e773a8fde8938c81dd402ba65b9201d60593e96c492651e889cc13f1415ebb53fac1131ae0b" +
    "d333c5ee6021672d9718ea31a8aebd0da0072f25d87dba6fc90ffd598ed4da35e44c398c454307" +
    "e8e33b8426143daec9f596836f97c8f74750e5975c64e2189f45def46b2a2b1247adc3652bf5c3" +
    "08055da9")]
[assembly: InternalsVisibleTo(
    "Microsoft.Azure.Storage.Extensions, PublicKey=" + 
    "0024000004800000940000000602000000240000525341310004000001000100b5fc90e7027f67" +
    "871e773a8fde8938c81dd402ba65b9201d60593e96c492651e889cc13f1415ebb53fac1131ae0b" +
    "d333c5ee6021672d9718ea31a8aebd0da0072f25d87dba6fc90ffd598ed4da35e44c398c454307" +
    "e8e33b8426143daec9f596836f97c8f74750e5975c64e2189f45def46b2a2b1247adc3652bf5c3" +
    "08055da9")]

[assembly: InternalsVisibleTo(
    "Microsoft.Azure.Storage.Test.NetCore2, PublicKey=" +
    "0024000004800000940000000602000000240000525341310004000001000100b5fc90e7027f67" +
    "871e773a8fde8938c81dd402ba65b9201d60593e96c492651e889cc13f1415ebb53fac1131ae0b" +
    "d333c5ee6021672d9718ea31a8aebd0da0072f25d87dba6fc90ffd598ed4da35e44c398c454307" +
    "e8e33b8426143daec9f596836f97c8f74750e5975c64e2189f45def46b2a2b1247adc3652bf5c3" +
    "08055da9")]

[assembly: InternalsVisibleTo("Microsoft.Azure.CosmosDB.Table, PublicKey=0024000004800000940000000602000000240000525341310004000001000100b5fc90e7027f67871e773a8fde8938c81dd402ba65b9201d60593e96c492651e889cc13f1415ebb53fac1131ae0bd333c5ee6021672d9718ea31a8aebd0da0072f25d87dba6fc90ffd598ed4da35e44c398c454307e8e33b8426143daec9f596836f97c8f74750e5975c64e2189f45def46b2a2b1247adc3652bf5c308055da9")]
[assembly: InternalsVisibleTo("Microsoft.Azure.CosmosDB.Table.Tests, PublicKey=0024000004800000940000000602000000240000525341310004000001000100b5fc90e7027f67871e773a8fde8938c81dd402ba65b9201d60593e96c492651e889cc13f1415ebb53fac1131ae0bd333c5ee6021672d9718ea31a8aebd0da0072f25d87dba6fc90ffd598ed4da35e44c398c454307e8e33b8426143daec9f596836f97c8f74750e5975c64e2189f45def46b2a2b1247adc3652bf5c308055da9")]
[assembly: InternalsVisibleTo("Microsoft.Azure.CosmosDB.Table.Tests.Gateway, PublicKey=0024000004800000940000000602000000240000525341310004000001000100b5fc90e7027f67871e773a8fde8938c81dd402ba65b9201d60593e96c492651e889cc13f1415ebb53fac1131ae0bd333c5ee6021672d9718ea31a8aebd0da0072f25d87dba6fc90ffd598ed4da35e44c398c454307e8e33b8426143daec9f596836f97c8f74750e5975c64e2189f45def46b2a2b1247adc3652bf5c308055da9")]
[assembly: InternalsVisibleTo("Microsoft.Azure.CosmosDB.Table, PublicKey=0024000004800000940000000602000000240000525341310004000001000100197c25d0a04f73cb271e8181dba1c0c713df8deebb25864541a66670500f34896d280484b45fe1ff6c29f2ee7aa175d8bcbd0c83cc23901a894a86996030f6292ce6eda6e6f3e6c74b3c5a3ded4903c951e6747e6102969503360f7781bf8bf015058eb89b7621798ccc85aaca036ff1bc1556bb7f62de15908484886aa8bbae")]
[assembly: InternalsVisibleTo("Microsoft.Azure.CosmosDB.Table.Tests, PublicKey=0024000004800000940000000602000000240000525341310004000001000100197c25d0a04f73cb271e8181dba1c0c713df8deebb25864541a66670500f34896d280484b45fe1ff6c29f2ee7aa175d8bcbd0c83cc23901a894a86996030f6292ce6eda6e6f3e6c74b3c5a3ded4903c951e6747e6102969503360f7781bf8bf015058eb89b7621798ccc85aaca036ff1bc1556bb7f62de15908484886aa8bbae")]
[assembly: InternalsVisibleTo("Microsoft.Azure.CosmosDB.Table.Tests.Gateway, PublicKey=0024000004800000940000000602000000240000525341310004000001000100197c25d0a04f73cb271e8181dba1c0c713df8deebb25864541a66670500f34896d280484b45fe1ff6c29f2ee7aa175d8bcbd0c83cc23901a894a86996030f6292ce6eda6e6f3e6c74b3c5a3ded4903c951e6747e6102969503360f7781bf8bf015058eb89b7621798ccc85aaca036ff1bc1556bb7f62de15908484886aa8bbae")] 
 
#else
[assembly: InternalsVisibleTo("Microsoft.Azure.Storage.Test.NetCore2")]
[assembly: InternalsVisibleTo("Microsoft.Azure.Storage.File")]
[assembly: InternalsVisibleTo("Microsoft.Azure.Storage.Queue")]
[assembly: InternalsVisibleTo("Microsoft.Azure.Storage.Blob")]
[assembly: InternalsVisibleTo("Microsoft.Azure.Storage.Test")]
[assembly: InternalsVisibleTo("Microsoft.Azure.Storage.Extensions")]
[assembly: InternalsVisibleTo("Microsoft.Azure.CosmosDB.Table")]
[assembly: InternalsVisibleTo("Microsoft.Azure.CosmosDB.Table.Tests")]
[assembly: InternalsVisibleTo("Microsoft.Azure.CosmosDB.Table.Tests.Gateway")]
[assembly: InternalsVisibleTo("Microsoft.Azure.CosmosDB.Table")]
[assembly: InternalsVisibleTo("Microsoft.Azure.CosmosDB.Table.Tests")]
[assembly: InternalsVisibleTo("Microsoft.Azure.CosmosDB.Table.Tests.Gateway")]

#endif

[assembly: NeutralResourcesLanguageAttribute("en-US")]
