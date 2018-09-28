// -----------------------------------------------------------------------------------------
// <copyright file="IPAddressOrRange.cs" company="Microsoft">
//    Copyright 2013 Microsoft Corporation
// 
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//      http://www.apache.org/licenses/LICENSE-2.0
// 
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.
// </copyright>
// -----------------------------------------------------------------------------------------

namespace Microsoft.WindowsAzure.Storage
{
    using System;
    using Microsoft.WindowsAzure.Storage.Core;
    using Microsoft.WindowsAzure.Storage.Core.Util;
#if WINDOWS_DESKTOP
    using System.Net;
    using System.Net.Sockets;
    using System.Globalization;
#elif WINDOWS_RT
    using Windows.Networking;
    using System.Globalization;
#endif

    /// <summary>
    /// Specifies either a single IP Address or a single range of IP Addresses (a minimum and a maximum, inclusive.)
    /// </summary>
    public class IPAddressOrRange
    {
        /// <summary>
        /// Initializes a new instance of the IPAddressOrRange class from a single IPAddress.
        /// </summary>
        /// <param name="address">The IP Address that the IPAddressOrRange object will represent.</param>
        public IPAddressOrRange(string address)
        {
            CommonUtility.AssertNotNull("address", address);

            // Validate that the address is IPv4
            IPAddressOrRange.AssertIPv4(address);

            this.Address = address;
            this.IsSingleAddress = true;
        }

        /// <summary>
        /// Initializes a new instance of the IPAddressOrRange class from two IPAddress objects, a minimum and a maximum.
        /// </summary>
        /// <param name="minimum">The minimum IP Address that the IPAddressOrRange object will use as a range boundary, inclusive.</param>
        /// <param name="maximum">The maximum IP Address that the IPAddressOrRange object will use as a range boundary, inclusive.</param>
        public IPAddressOrRange(string minimum, string maximum)
        {
            CommonUtility.AssertNotNull("minimum", minimum);
            CommonUtility.AssertNotNull("maximum", maximum);

            // Validate that the addresses are IPv4
            IPAddressOrRange.AssertIPv4(minimum);
            IPAddressOrRange.AssertIPv4(maximum);
            
            this.MinimumAddress = minimum;
            this.MaximumAddress = maximum;
            this.IsSingleAddress = false;
        }

        /// <summary>
        /// The IP Address.
        /// Returns null if this object represents a range of IP addresses.
        /// </summary>
        public string Address
        {
            get;
            private set;
        }

        /// <summary>
        /// The minimum IP Address for the range, inclusive.
        /// Returns null if this object represents a single IP address.
        /// </summary>
        public string MinimumAddress
        {
            get;
            private set;
        }

        /// <summary>
        /// The maximum IP Address for the range, inclusive.
        /// Returns null if this object represents a single IP address.
        /// </summary>
        public string MaximumAddress
        {
            get;
            private set;
        }

        /// <summary>
        /// True if this object represents a single IP Address, false if it represents a range.
        /// </summary>
        public bool IsSingleAddress
        {
            get;
            private set;
        }

        /// <summary>
        /// Provides a string representation of this IPAddressOrRange object.
        /// </summary>
        /// <returns>The string representation of this IPAddressOrRange object.</returns>
        public override string ToString()
        {
            if (this.IsSingleAddress)
            {
                return Address;
            }
            else
            {
                return MinimumAddress + "-" + MaximumAddress;
            }
        }

        /// <summary>
        /// Assert that an IP address is in IPv4 format.
        /// </summary>
        /// <param name="address">The IP address to assert.</param>
        private static void AssertIPv4(string address)
        {
#if WINDOWS_DESKTOP
            IPAddress parsedAddress;
            
            if (IPAddress.TryParse(address, out parsedAddress) == false)
            {
                throw new ArgumentException(SR.InvalidIPAddress);
            }
            
            if (parsedAddress.AddressFamily != AddressFamily.InterNetwork)
            {
                 throw new ArgumentException(String.Format(CultureInfo.CurrentCulture, SR.IPMustBeIPV4InSAS, address));
            }
#elif WINDOWS_RT
            HostName parsedAddress;

            try
            {
                parsedAddress = new HostName(address);
            }
            catch (ArgumentException)
            {
                throw new ArgumentException(SR.InvalidIPAddress);
            }

            if (parsedAddress.Type != HostNameType.Ipv4)
            {
                throw new ArgumentException(String.Format(CultureInfo.CurrentCulture, SR.IPMustBeIPV4InSAS, address));
            }
#endif
        }
    }
}
