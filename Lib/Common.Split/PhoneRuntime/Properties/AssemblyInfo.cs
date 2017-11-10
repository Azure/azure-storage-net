using System.Reflection;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("Microsoft.Azure.Storage.Common.dll")]
[assembly: AssemblyDescription("Azure Storage CommonSDK for Windows Phone Runtime")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Microsoft")]
[assembly: AssemblyProduct("Azure Storage")]
[assembly: AssemblyCopyright("Copyright © 2013 Microsoft Corp.")]
[assembly: AssemblyTrademark("Microsoft ® is a registered trademark of Microsoft Corporation.")]
[assembly: AssemblyCulture("")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Build and Revision Numbers 
// by using the '*' as shown below:
[assembly: AssemblyVersion("8.6.0")]
[assembly: AssemblyFileVersion("8.6.0")]
[assembly: AssemblyInformationalVersion("8.6.0.0-preview")]

[assembly: NeutralResourcesLanguageAttribute("en-US")]
[assembly: ComVisible(false)]

#if SIGN
[assembly: InternalsVisibleTo(
    "Microsoft.Azure.Storage.Blob, PublicKey=" +
    "0024000004800000940000000602000000240000525341310004000001000100b5fc90e7027f67" +
    "871e773a8fde8938c81dd402ba65b9201d60593e96c492651e889cc13f1415ebb53fac1131ae0b" +
    "d333c5ee6021672d9718ea31a8aebd0da0072f25d87dba6fc90ffd598ed4da35e44c398c454307" +
    "e8e33b8426143daec9f596836f97c8f74750e5975c64e2189f45def46b2a2b1247adc3652bf5c3" +
    "08055da9")]
[assembly: InternalsVisibleTo(
    "Microsoft.Azure.Storage.Queue, PublicKey=" +
    "0024000004800000940000000602000000240000525341310004000001000100b5fc90e7027f67" +
    "871e773a8fde8938c81dd402ba65b9201d60593e96c492651e889cc13f1415ebb53fac1131ae0b" +
    "d333c5ee6021672d9718ea31a8aebd0da0072f25d87dba6fc90ffd598ed4da35e44c398c454307" +
    "e8e33b8426143daec9f596836f97c8f74750e5975c64e2189f45def46b2a2b1247adc3652bf5c3" +
    "08055da9")]
[assembly: InternalsVisibleTo(
    "Microsoft.Azure.Storage.Table, PublicKey=" +
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
    "Microsoft.Azure.Storage.Test, PublicKey=" +
    "0024000004800000940000000602000000240000525341310004000001000100b5fc90e7027f67" +
    "871e773a8fde8938c81dd402ba65b9201d60593e96c492651e889cc13f1415ebb53fac1131ae0b" +
    "d333c5ee6021672d9718ea31a8aebd0da0072f25d87dba6fc90ffd598ed4da35e44c398c454307" +
    "e8e33b8426143daec9f596836f97c8f74750e5975c64e2189f45def46b2a2b1247adc3652bf5c3" +
    "08055da9")]
#else
[assembly: InternalsVisibleTo("Microsoft.Azure.Storage.File")]
[assembly: InternalsVisibleTo("Microsoft.Azure.Storage.Blob")]
[assembly: InternalsVisibleTo("Microsoft.Azure.Storage.Table")]
[assembly: InternalsVisibleTo("Microsoft.Azure.Storage.Queue")]
[assembly: InternalsVisibleTo("Microsoft.Azure.Storage.Test")]
#endif