// -----------------------------------------------------------------------------------------
// <copyright file="Assert.cs" company="Microsoft">
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

namespace Microsoft.VisualStudio.TestPlatform.UnitTestFramework
{
    using System;
    using System.Globalization;

    /// <summary>
    /// Verifies conditions in unit tests using true/false propositions.
    /// </summary>
    public partial class Assert
    {
        /// <summary>
        /// Verifies that two specified objects are equal. The assertion fails if the objects are not equal.
        /// </summary>
        /// <param name="expected">The first object to compare. This is the object the unit test expects.</param>
        /// <param name="actual">The second object to compare. This is the object the unit test produced.</param>
        public static void AreEqual(object expected, object actual)
        {
            var xType = expected.GetType();
            var yType = actual.GetType();

            if(xType != yType)
            {
                //In MsTest, Uri compare with string works this way, xUnit test by default fails if type is different
                if(expected.ToString() == actual.ToString())
                {
                    return;
                }

                if(xType == typeof(Uri) && yType == typeof(string))
                {
                    Xunit.Assert.Equal((Uri) expected, new Uri((string) actual));
                    return;
                }

                if (xType == typeof(string) && yType == typeof(Uri))
                {
                    Xunit.Assert.Equal(new Uri((string) expected), (Uri) actual);
                    return;
                }
            }

            Xunit.Assert.Equal(expected, actual);
        }

        /// <summary>
        /// Verifies that two specified strings are equal, ignoring case or not as specified. 
        /// The assertion fails if they are not equal.
        /// </summary>
        /// <param name="expected">The first string to compare. This is the string the unit test expects.</param>
        /// <param name="actual">The second string to compare. This is the string the unit test produced.</param>
        /// <param name="ignoreCase">A Boolean value that indicates a case-sensitive or insensitive comparison. 
        /// true indicates a case-insensitive comparison.</param>
        public static void AreEqual(string expected, string actual, bool ignoreCase)
        {
            Xunit.Assert.Equal(expected, actual, ignoreCase);
        }

        /// <summary>
        /// Verifies that two specified doubles are equal, or within the specified accuracy of each other. 
        /// The assertion fails if they are not within the specified accuracy of each other.
        /// </summary>
        /// <param name="expected">The first double to compare. This is the double the unit test expects.</param>
        /// <param name="actual">The second double to compare. This is the double the unit test produced.</param>
        /// <param name="delta">The required accuracy. The assertion will fail only if expected is different from
        /// actual by more than delta.</param>
        public static void AreEqual(double expected, double actual, double delta)
        {
            Xunit.Assert.True(actual >= expected - delta && actual <= expected + delta);
        }

        /// <summary>
        /// Verifies that two specified floats are equal, or within the specified accuracy of each other. 
        /// The assertion fails if they are not within the specified accuracy of each other.
        /// </summary>
        /// <param name="expected">The first float to compare. This is the float the unit test expects.</param>
        /// <param name="actual">The second float to compare. This is the float the unit test produced.</param>
        /// <param name="delta">The required accuracy. The assertion will fail only if expected is different from
        /// actual by more than delta.</param>
        public static void AreEqual(float expected, float actual, float delta)
        {
            Xunit.Assert.True(actual >= expected - delta && actual <= expected + delta);
        }

        /// <summary>
        /// Verifies that two specified objects are equal. The assertion fails if the objects are not equal. 
        /// Displays a message if the assertion fails.
        /// </summary>
        /// <param name="expected">The first object to compare. This is the object the unit test expects.</param>
        /// <param name="actual">The second object to compare. This is the object the unit test produced.</param>
        /// <param name="message">A message to display if the assertion fails. This message can be seen in the unit test results.</param>
        public static void AreEqual(object expected, object actual, string message)
        {
            Xunit.Assert.Equal(expected, actual);
        }

        /// <summary>
        /// Verifies that two specified floats are equal, or within the specified accuracy of each other. 
        /// The assertion fails if they are not within the specified accuracy of each other. 
        /// Displays a message if the assertion fails.
        /// </summary>
        /// <param name="expected">The first float to compare. This is the float the unit test expects.</param>
        /// <param name="actual">The second float to compare. This is the float the unit test produced.</param>
        /// <param name="delta">The required accuracy. The assertion will fail only if expected is different from actual by more than delta.</param>
        /// <param name="message">A message to display if the assertion fails. This message can be seen in the unit test results.</param>
        public static void AreEqual(float expected, float actual, float delta, string message)
        {
            AreEqual(expected, actual, delta);
        }

        /// <summary>
        /// Verifies that two specified doubles are equal, or within the specified accuracy of each other. 
        /// The assertion fails if they are not within the specified accuracy of each other. 
        /// Displays a message if the assertion fails.
        /// </summary>
        /// <param name="expected">The first double to compare. This is the double the unit test expects.</param>
        /// <param name="actual">The second double to compare. This is the double the unit test produced.</param>
        /// <param name="delta">The required accuracy. The assertion will fail only if expected is different from actual by more than delta.</param>
        /// <param name="message">A message to display if the assertion fails. This message can be seen in the unit test results.</param>
        public static void AreEqual(double expected, double actual, double delta, string message)
        {
            AreEqual(expected, actual, delta);
        }

        /// <summary>
        /// Verifies that two specified strings are equal, ignoring case or not as specified, 
        /// and using the culture info specified. The assertion fails if they are not equal.
        /// </summary>
        /// <param name="expected">The first string to compare. This is the string the unit test expects.</param>
        /// <param name="actual">The second string to compare. This is the string the unit test produced.</param>
        /// <param name="ignoreCase">A Boolean value that indicates a case-sensitive or insensitive comparison. 
        /// true indicates a case-insensitive comparison.</param>
        /// <param name="culture">A <see cref="CultureInfo"/> object that supplies culture-specific comparison information.</param>
        public static void AreEqual(string expected, string actual, bool ignoreCase, CultureInfo culture)
        {
            //CultureInfo currentCulture = CultureInfo.CurrentCulture;
            //CultureInfo.CurrentCulture = culture;

            try
            {
                AreEqual(expected, actual, ignoreCase);
            }
            finally
            {
                //CultureInfo.CurrentCulture = currentCulture;
            }
        }

        /// <summary>
        /// Verifies that two specified strings are equal, ignoring case or not as specified. 
        /// The assertion fails if they are not equal. Displays a message if the assertion fails.
        /// </summary>
        /// <param name="expected">The first string to compare. This is the string the unit test expects.</param>
        /// <param name="actual">The second string to compare. This is the string the unit test produced.</param>
        /// <param name="ignoreCase">A Boolean value that indicates a case-sensitive or insensitive comparison. 
        /// true indicates a case-insensitive comparison.</param>
        /// <param name="message">A message to display if the assertion fails. This message can be seen in the unit test results.</param>
        public static void AreEqual(string expected, string actual, bool ignoreCase, string message)
        {
            Xunit.Assert.Equal(expected, actual, ignoreCase);
        }

        /// <summary>
        /// Verifies that two specified objects are equal. The assertion fails if the objects are not equal. 
        /// Displays a message if the assertion fails, and applies the specified formatting to it.
        /// </summary>
        /// <param name="expected">The first object to compare. This is the object the unit test expects.</param>
        /// <param name="actual">The second object to compare. This is the object the unit test produced.</param>
        /// <param name="message">A message to display if the assertion fails. This message can be seen in the unit test results.</param>
        /// <param name="parameters">An array of parameters to use when formatting message.</param>
        public static void AreEqual(object expected, object actual, string message, params object[] parameters)
        {
            Xunit.Assert.Equal(expected, actual);
        }

        /// <summary>
        /// Verifies that two specified strings are equal, ignoring case or not as specified, 
        /// and using the culture info specified. 
        /// The assertion fails if they are not equal. Displays a message if the assertion fails.
        /// </summary>
        /// <param name="expected">The first string to compare. This is the string the unit test expects.</param>
        /// <param name="actual">The second string to compare. This is the string the unit test produced.</param>
        /// <param name="ignoreCase">A Boolean value that indicates a case-sensitive or insensitive comparison. 
        /// true indicates a case-insensitive comparison.</param>
        /// <param name="culture">A <see cref="CultureInfo"/> object that supplies culture-specific comparison information.</param>
        /// <param name="message">A message to display if the assertion fails. This message can be seen in the unit test results.</param>
        public static void AreEqual(string expected, string actual, bool ignoreCase, CultureInfo culture, string message)
        {
            AreEqual(expected, actual, ignoreCase, culture);
        }

        /// <summary>
        /// Verifies that two specified doubles are equal, or within the specified accuracy of each other. 
        /// The assertion fails if they are not within the specified accuracy of each other. 
        /// Displays a message if the assertion fails, and applies the specified formatting to it.
        /// </summary>
        /// <param name="expected">The first double to compare. This is the double the unit tests expects.</param>
        /// <param name="actual">The second double to compare. This is the double the unit test produced.</param>
        /// <param name="delta">The required accuracy. The assertion will fail only if expected is different from 
        /// actual by more than delta.</param>
        /// <param name="message">A message to display if the assertion fails. This message can be seen in the 
        /// unit test results.</param>
        /// <param name="parameters">An array of parameters to use when formatting message.</param>
        public static void AreEqual(double expected, double actual, double delta, string message, params object[] parameters)
        {
            AreEqual(expected, actual, delta);
        }

        /// <summary>
        /// Verifies that two specified strings are equal, ignoring case or not as specified. 
        /// The assertion fails if they are not equal. 
        /// Displays a message if the assertion fails, and applies the specified formatting to it.
        /// </summary>
        /// <param name="expected">The first string to compare. This is the string the unit test expects.</param>
        /// <param name="actual">The second string to compare. This is the string the unit test produced.</param>
        /// <param name="ignoreCase">A Boolean value that indicates a case-sensitive or insensitive comparison. 
        /// true indicates a case-insensitive comparison.</param>
        /// <param name="message">A message to display if the assertion fails. This message can be seen in the unit test results.</param>
        /// <param name="parameters">An array of parameters to use when formatting message.</param>
        public static void AreEqual(string expected, string actual, bool ignoreCase, string message, params object[] parameters)
        {
            Xunit.Assert.Equal(expected, actual, ignoreCase);
        }

        /// <summary>
        /// Verifies that two specified floats are equal, or within the specified accuracy of each other. 
        /// The assertion fails if they are not within the specified accuracy of each other. 
        /// Displays a message if the assertion fails, and applies the specified formatting to it.
        /// </summary>
        /// <param name="expected">The first float to compare. This is the float the unit tests expects.</param>
        /// <param name="actual">The second float to compare. This is the float the unit test produced.</param>
        /// <param name="delta">The required accuracy. The assertion will fail only if expected is different from 
        /// actual by more than delta.</param>
        /// <param name="message">A message to display if the assertion fails. This message can be seen in the 
        /// unit test results.</param>
        /// <param name="parameters">An array of parameters to use when formatting message.</param>
        public static void AreEqual(float expected, float actual, float delta, string message, params object[] parameters)
        {
            AreEqual(expected, actual, delta);
        }

        /// <summary>
        /// Verifies that two specified strings are equal, ignoring case or not as specified, 
        /// and using the culture info specified. The assertion fails if they are not equal. 
        /// Displays a message if the assertion fails, and applies the specified formatting to it.
        /// </summary>
        /// <param name="expected">The first string to compare. This is the string the unit test expects.</param>
        /// <param name="actual">The second string to compare. This is the string the unit test produced.</param>
        /// <param name="ignoreCase">A Boolean value that indicates a case-sensitive or insensitive comparison. 
        /// true indicates a case-insensitive comparison.</param>
        /// <param name="culture">A <see cref="CultureInfo"/> object that supplies culture-specific comparison information.</param>
        /// <param name="message">A message to display if the assertion fails. This message can be seen in the unit test results.</param>
        /// <param name="parameters">An array of parameters to use when formatting message.</param>
        public static void AreEqual(string expected, string actual, bool ignoreCase, CultureInfo culture, string message, params object[] parameters)
        {
            AreEqual(expected, actual, ignoreCase, culture);
        }

        /// <summary>
        /// Verifies that two specified generic type data are equal by using the equality operator. 
        /// The assertion fails if they are not equal.
        /// </summary>
        /// <param name="expected">The first generic type data to compare. This is the generic type data the unit test expects.</param>
        /// <param name="actual">The second generic type data to compare. This is the generic type data the unit test produced.</param>
        public static void AreEqual<T>(T expected, T actual)
        {
            Xunit.Assert.Equal(expected, actual);
        }

        /// <summary>
        /// Verifies that two specified generic type data are equal by using the equality operator. 
        /// The assertion fails if they are not equal. Displays a message if the assertion fails.
        /// </summary>
        /// <param name="expected">The first generic type data to compare. This is the generic type data the unit test expects.</param>
        /// <param name="actual">The second generic type data to compare. This is the generic type data the unit test produced.</param>
        /// <param name="message">A message to display if the assertion fails. This message can be seen in the unit test results.</param>
        public static void AreEqual<T>(T expected, T actual, string message)
        {
            Xunit.Assert.Equal(expected, actual);
        }

        /// <summary>
        /// Verifies that two specified generic type data are equal by using the equality operator. 
        /// The assertion fails if they are not equal. 
        /// Displays a message if the assertion fails, and applies the specified formatting to it.
        /// </summary>
        /// <param name="expected">The first generic type data to compare. This is the generic type data the unit test expects.</param>
        /// <param name="actual">The second generic type data to compare. This is the generic type data the unit test produced.</param>
        /// <param name="message">A message to display if the assertion fails. This message can be seen in the unit test results.</param>
        /// <param name="parameters">An array of parameters to use when formatting message.</param>
        public static void AreEqual<T>(T expected, T actual, string message, params object[] parameters)
        {
            Xunit.Assert.Equal(expected, actual);
        }

        /// <summary>
        /// Verifies that two specified objects are not equal. The assertion fails if the objects are equal.
        /// </summary>
        /// <param name="notExpected">The first object to compare. This is the object the unit test expects not to match actual.</param>
        /// <param name="actual">The second object to compare. This is the object the unit test produced.</param>
        public static void AreNotEqual(object notExpected, object actual)
        {
            Xunit.Assert.NotEqual(notExpected, actual);
        }

        /// <summary>
        /// Verifies that two specified floats are not equal, and not within the specified accuracy of each other. 
        /// The assertion fails if they are equal or within the specified accuracy of each other.
        /// </summary>
        /// <param name="notExpected">The first float to compare. This is the float the unit test expects not to match actual.</param>
        /// <param name="actual">The second float to compare. This is the float the unit test produced.</param>
        /// <param name="delta">The required inaccuracy. The assertion fails only if notExpected is equal to actual or different from it by less than delta.</param>
        public static void AreNotEqual(float notExpected, float actual, float delta)
        {
            Xunit.Assert.False(actual >= notExpected - delta && actual <= notExpected + delta);
        }

        /// <summary>
        /// Verifies that two specified strings are not equal, ignoring case or not as specified. The assertion fails if they are equal.
        /// </summary>
        /// <param name="notExpected">The first string to compare. This is the string the unit test expects not to match actual.</param>
        /// <param name="actual">The second string to compare. This is the string the unit test produced.</param>
        /// <param name="ignoreCase">A Boolean value that indicates a case-sensitive or insensitive comparison. true indicates a case-insensitive comparison.</param>
        public static void AreNotEqual(string notExpected, string actual, bool ignoreCase)
        {
            if(ignoreCase)
            {
                Xunit.Assert.NotEqual(notExpected.ToLowerInvariant(), actual.ToLowerInvariant());
            }
            else
            {
                Xunit.Assert.NotEqual(notExpected, actual);
            }
        }

        /// <summary>
        /// Verifies that two specified doubles are not equal, and not within the specified accuracy of each other. 
        /// The assertion fails if they are equal or within the specified accuracy of each other.
        /// </summary>
        /// <param name="notExpected">The first double to compare. This is the double the unit test expects not to match actual.</param>
        /// <param name="actual">The second double to compare. This is the double the unit test produced.</param>
        /// <param name="delta">The required inaccuracy. The assertion fails only if notExpected is equal to actual or different from it by less than delta.</param>
        public static void AreNotEqual(double notExpected, double actual, double delta)
        {
            Xunit.Assert.False(actual >= notExpected - delta && actual <= notExpected + delta);
        }

        /// <summary>
        /// Verifies that two specified objects are not equal. The assertion fails if the objects are equal. Displays a message if the assertion fails.
        /// </summary>
        /// <param name="notExpected">The first object to compare. This is the object the unit test expects not to match actual.</param>
        /// <param name="actual">The second object to compare. This is the object the unit test produced.</param>
        /// <param name="message">A message to display if the assertion fails. This message can be seen in the unit test results.</param>
        public static void AreNotEqual(object notExpected, object actual, string message)
        {
            Xunit.Assert.NotEqual(notExpected, actual);
        }

        /// <summary>
        /// Verifies that two specified doubles are not equal, and not within the specified accuracy of each other. 
        /// The assertion fails if they are equal or within the specified accuracy of each other. 
        /// Displays a message if the assertion fails.
        /// </summary>
        /// <param name="notExpected">The first double to compare. This is the double the unit test expects not to match actual.</param>
        /// <param name="actual">The second double to compare. This is the double the unit test produced.</param>
        /// <param name="delta">The required inaccuracy. The assertion fails only if notExpected is equal to actual or different from it by less than delta.</param>
        /// <param name="message">A message to display if the assertion fails. This message can be seen in the unit test results.</param>
        public static void AreNotEqual(double notExpected, double actual, double delta, string message)
        {
            Xunit.Assert.NotEqual(notExpected, actual);
        }

        /// <summary>
        /// Verifies that two specified strings are not equal, ignoring case or not as specified. 
        /// The assertion fails if they are equal. Displays a message if the assertion fails.
        /// </summary>
        /// <param name="notExpected">The first string to compare. This is the string the unit test expects not to match actual.</param>
        /// <param name="actual">The second string to compare. This is the string the unit test produced.</param>
        /// <param name="ignoreCase">A Boolean value that indicates a case-sensitive or insensitive comparison. true indicates a case-insensitive comparison.</param>
        /// <param name="message">A message to display if the assertion fails. This message can be seen in the unit test results.</param>
        public static void AreNotEqual(string notExpected, string actual, bool ignoreCase, string message)
        {
            if(ignoreCase)
            {
                Xunit.Assert.NotEqual(notExpected.ToLowerInvariant(), actual.ToLowerInvariant());
            }
            else
            {
                Xunit.Assert.NotEqual(notExpected, actual);
            }
        }

        /// <summary>
        /// Verifies that two specified strings are not equal, ignoring case or not as specified, 
        /// and using the culture info specified. The assertion fails if they are equal.
        /// </summary>
        /// <param name="notExpected">The first string to compare. This is the string the unit test expects not to match actual.</param>
        /// <param name="actual">The second string to compare. This is the string the unit test produced.</param>
        /// <param name="ignoreCase">A Boolean value that indicates a case-sensitive or insensitive comparison. true indicates a case-insensitive comparison.</param>
        /// <param name="culture">A CultureInfo object that supplies culture-specific comparison information.</param>
        public static void AreNotEqual(string notExpected, string actual, bool ignoreCase, CultureInfo culture)
        {
           // CultureInfo currentCulture = CultureInfo.CurrentCulture;
           // CultureInfo.CurrentCulture = culture;

            try
            {
                AreNotEqual(notExpected, actual, ignoreCase);
            }
            finally
            {
              //  CultureInfo.CurrentCulture = currentCulture;
            }
        }

        /// <summary>
        /// Verifies that two specified floats are not equal, and not within the specified accuracy of each other. 
        /// The assertion fails if they are equal or within the specified accuracy of each other. 
        /// Displays a message if the assertion fails.
        /// </summary>
        /// <param name="notExpected">The first float to compare. This is the float the unit test expects not to match actual.</param>
        /// <param name="actual">The second float to compare. This is the float the unit test produced.</param>
        /// <param name="delta">The required inaccuracy. The assertion fails only if notExpected is equal to actual or different from it by less than delta.</param>
        /// <param name="message">A message to display if the assertion fails. This message can be seen in the unit test results.</param>
        public static void AreNotEqual(float notExpected, float actual, float delta, string message)
        {
            Xunit.Assert.False(actual >= notExpected - delta && actual <= notExpected + delta);
        }

        /// <summary>
        /// Verifies that two specified objects are not equal. The assertion fails if the objects are equal. 
        /// Displays a message if the assertion fails, and applies the specified formatting to it.
        /// </summary>
        /// <param name="notExpected">The first object to compare. This is the object the unit test expects not to match actual.</param>
        /// <param name="actual">The second object to compare. This is the object the unit test produced.</param>
        /// <param name="message">A message to display if the assertion fails. This message can be seen in the unit test results.</param>
        /// <param name="parameters">An array of parameters to use when formatting message.</param>
        public static void AreNotEqual(object notExpected, object actual, string message, params object[] parameters)
        {
            Xunit.Assert.NotEqual(notExpected, actual);
        }

        /// <summary>
        /// Verifies that two specified strings are not equal, ignoring case or not as specified, and using the culture info specified. 
        /// The assertion fails if they are equal. Displays a message if the assertion fails.
        /// </summary>
        /// <param name="notExpected">The first string to compare. This is the string the unit test expects not to match actual.</param>
        /// <param name="actual">The second string to compare. This is the string the unit test produced.</param>
        /// <param name="ignoreCase">A Boolean value that indicates a case-sensitive or insensitive comparison. true indicates a case-insensitive comparison.</param>
        /// <param name="culture">A CultureInfo object that supplies culture-specific comparison information.</param>
        /// <param name="message">A message to display if the assertion fails. This message can be seen in the unit test results.</param>
        public static void AreNotEqual(string notExpected, string actual, bool ignoreCase, CultureInfo culture, string message)
        {
            AreNotEqual(notExpected, actual, ignoreCase, culture);
        }

        /// <summary>
        /// Verifies that two specified strings are not equal, ignoring case or not as specified. The assertion fails if they are equal. 
        /// Displays a message if the assertion fails, and applies the specified formatting to it.
        /// </summary>
        /// <param name="notExpected">The first string to compare. This is the string the unit test expects not to match actual.</param>
        /// <param name="actual">The second string to compare. This is the string the unit test produced.</param>
        /// <param name="ignoreCase">A Boolean value that indicates a case-sensitive or insensitive comparison. true indicates a case-insensitive comparison.</param>
        /// <param name="message">A message to display if the assertion fails. This message can be seen in the unit test results.</param>
        /// <param name="parameters">An array of parameters to use when formatting message.</param>
        public static void AreNotEqual(string notExpected, string actual, bool ignoreCase, string message, params object[] parameters)
        {
            AreNotEqual(notExpected, actual, ignoreCase);
        }

        /// <summary>
        /// Verifies that two specified doubles are not equal, and not within the specified accuracy of each other. 
        /// The assertion fails if they are equal or within the specified accuracy of each other. 
        /// Displays a message if the assertion fails, and applies the specified formatting to it.
        /// </summary>
        /// <param name="notExpected">The first double to compare. This is the double the unit test expects not to match actual.</param>
        /// <param name="actual">The second double to compare. This is the double the unit test produced.</param>
        /// <param name="delta">The required inaccuracy. The assertion will fail only if notExpected is equal to actual or different from it by less than delta.</param>
        /// <param name="message">A message to display if the assertion fails. This message can be seen in the unit test results.</param>
        /// <param name="parameters">An array of parameters to use when formatting message.</param>
        public static void AreNotEqual(double notExpected, double actual, double delta, string message, params object[] parameters)
        {
            AreNotEqual(notExpected, actual, delta);
        }

        /// <summary>
        /// Verifies that two specified floats are not equal, and not within the specified accuracy of each other. 
        /// The assertion fails if they are equal or within the specified accuracy of each other. 
        /// Displays a message if the assertion fails, and applies the specified formatting to it.
        /// </summary>
        /// <param name="notExpected">The first float to compare. This is the float the unit test expects not to match actual.</param>
        /// <param name="actual">The second float to compare. This is the float the unit test produced.</param>
        /// <param name="delta">The required inaccuracy. The assertion will fail only if notExpected is equal to actual or different from it by less than delta.</param>
        /// <param name="message">A message to display if the assertion fails. This message can be seen in the unit test results.</param>
        /// <param name="parameters">An array of parameters to use when formatting message.</param>
        public static void AreNotEqual(float notExpected, float actual, float delta, string message, params object[] parameters)
        {
            AreNotEqual(notExpected, actual, delta);
        }

        /// <summary>
        /// Verifies that two specified strings are not equal, ignoring case or not as specified, 
        /// and using the culture info specified. The assertion fails if they are equal. 
        /// Displays a message if the assertion fails, and applies the specified formatting to it.
        /// </summary>
        /// <param name="notExpected">The first string to compare. This is the string the unit test expects not to match actual.</param>
        /// <param name="actual">The second string to compare. This is the string the unit test produced.</param>
        /// <param name="ignoreCase">A Boolean value that indicates a case-sensitive or insensitive comparison. true indicates a case-insensitive comparison.</param>
        /// <param name="culture">A CultureInfo object that supplies culture-specific comparison information.</param>
        /// <param name="message">A message to display if the assertion fails. This message can be seen in the unit test results.</param>
        /// <param name="parameters">An array of parameters to use when formatting message.</param>
        public static void AreNotEqual(string notExpected, string actual, bool ignoreCase, CultureInfo culture, string message, params object[] parameters)
        {
            AreNotEqual(notExpected, actual, ignoreCase, culture);
        }

        /// <summary>
        /// Verifies that two specified generic type data are not equal. The assertion fails if they are equal.
        /// </summary>
        /// <param name="notExpected">The first generic type data to compare. This is the generic type data the unit test expects not to match actual.</param>
        /// <param name="actual">The second generic type data to compare. This is the generic type data the unit test produced.</param>
        public static void AreNotEqual<T>(T notExpected, T actual)
        {
            Xunit.Assert.NotEqual(notExpected, actual);
        }

        /// <summary>
        /// Verifies that two specified generic type data are not equal. The assertion fails if they are equal. Displays a message if the assertion fails.
        /// </summary>
        /// <param name="notExpected">The first generic type data to compare. This is the generic type data the unit test expects not to match actual.</param>
        /// <param name="actual">The second generic type data to compare. This is the generic type data the unit test produced.</param>
        /// <param name="message">A message to display if the assertion fails. This message can be seen in the unit test results.</param>
        public static void AreNotEqual<T>(T notExpected, T actual, string message)
        {
            Xunit.Assert.NotEqual(notExpected, actual);
        }

        /// <summary>
        /// Verifies that two specified generic type data are not equal. The assertion fails if they are equal. 
        /// Displays a message if the assertion fails, and applies the specified formatting to it.
        /// </summary>
        /// <param name="notExpected">The first generic type data to compare. This is the generic type data the unit test expects not to match actual.</param>
        /// <param name="actual">The second generic type data to compare. This is the generic type data the unit test produced.</param>
        /// <param name="message">A message to display if the assertion fails. This message can be seen in the unit test results.</param>
        /// <param name="parameters">An array of parameters to use when formatting message.</param>
        public static void AreNotEqual<T>(T notExpected, T actual, string message, params object[] parameters)
        {
            Xunit.Assert.NotEqual(notExpected, actual);
        }

        /// <summary>
        /// Verifies that two specified object variables refer to different objects. The assertion fails if they refer to the same object.
        /// </summary>
        /// <param name="notExpected">The first object to compare. This is the object the unit test expects not to match actual.</param>
        /// <param name="actual">The second object to compare. This is the object the unit test produced.</param>
        public static void AreNotSame(object notExpected, object actual)
        {
            Xunit.Assert.NotSame(notExpected, actual);
        }

        /// <summary>
        /// Verifies that two specified object variables refer to different objects. 
        /// The assertion fails if they refer to the same object. Displays a message if the assertion fails.
        /// </summary>
        /// <param name="notExpected">The first object to compare. This is the object the unit test expects not to match actual.</param>
        /// <param name="actual">The second object to compare. This is the object the unit test produced.</param>
        /// <param name="message">A message to display if the assertion fails. This message can be seen in the unit test results.</param>
        public static void AreNotSame(object notExpected, object actual, string message)
        {
            Xunit.Assert.NotSame(notExpected, actual);
        }

        /// <summary>
        /// Verifies that two specified object variables refer to different objects. 
        /// The assertion fails if they refer to the same object. Displays a message if the assertion fails, 
        /// and applies the specified formatting to it.
        /// </summary>
        /// <param name="notExpected">The first object to compare. This is the object the unit test expects not to match actual.</param>
        /// <param name="actual">The second object to compare. This is the object the unit test produced.</param>
        /// <param name="message">A message to display if the assertion fails. This message can be seen in the unit test results.</param>
        /// <param name="parameters">An array of parameters to use when formatting message.</param>
        public static void AreNotSame(object notExpected, object actual, string message, params object[] parameters)
        {
            Xunit.Assert.NotSame(notExpected, actual);
        }

        /// <summary>
        /// Verifies that two specified object variables refer to the same object. The assertion fails if they refer to different objects.
        /// </summary>
        /// <param name="expected">The first object to compare. This is the object the unit test expects.</param>
        /// <param name="actual">The second object to compare. This is the object the unit test produced.</param>
        public static void AreSame(object expected, object actual)
        {
            Xunit.Assert.Same(expected, actual);
        }

        /// <summary>
        /// Verifies that two specified object variables refer to the same object. 
        /// The assertion fails if they refer to different objects. Displays a message if the assertion fails.
        /// </summary>
        /// <param name="expected">The first object to compare. This is the object the unit test expects.</param>
        /// <param name="actual">The second object to compare. This is the object the unit test produced.</param>
        /// <param name="message">A message to display if the assertion fails. This message can be seen in the unit test results.</param>
        public static void AreSame(object expected, object actual, string message)
        {
            Xunit.Assert.Same(expected, actual);
        }

        /// <summary>
        /// Verifies that two specified object variables refer to the same object. 
        /// The assertion fails if they refer to different objects. 
        /// Displays a message if the assertion fails, and applies the specified formatting to it.
        /// </summary>
        /// <param name="expected">The first object to compare. This is the object the unit test expects.</param>
        /// <param name="actual">The second object to compare. This is the object the unit test produced.</param>
        /// <param name="message">A message to display if the assertion fails. This message can be seen in the unit test results.</param>
        /// <param name="parameters">An array of parameters to use when formatting message.</param>
        public static void AreSame(object expected, object actual, string message, params object[] parameters)
        {
            Xunit.Assert.Same(expected, actual);
        }

        /// <summary>
        /// Fails the assertion without checking any conditions.
        /// </summary>
        public static void Fail()
        {
            Xunit.Assert.True(false);
        }

        /// <summary>
        /// Fails the assertion without checking any conditions. Displays a message.
        /// </summary>
        /// <param name="message">A message to display. This message can be seen in the unit test results.</param>
        public static void Fail(string message)
        {
            Xunit.Assert.True(false, message);
        }

        /// <summary>
        /// Fails the assertion without checking any conditions. Displays a message, and applies the specified formatting to it.
        /// </summary>
        /// <param name="message">A message to display. This message can be seen in the unit test results.</param>
        /// <param name="parameters">An array of parameters to use when formatting message.</param>
        public static void Fail(string message, params object[] parameters)
        {
            Xunit.Assert.True(false, String.Format(message, parameters));
        }

        /// <summary>
        /// Indicates that the assertion cannot be verified.
        /// </summary>
        public static void Inconclusive()
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Indicates that the assertion can not be verified. Displays a message.
        /// </summary>
        /// <param name="message">A message to display. This message can be seen in the unit test results.</param>
        public static void Inconclusive(string message)
        {
            throw new NotSupportedException(message);
        }

        /// <summary>
        /// Indicates that an assertion can not be verified. Displays a message, and applies the specified formatting to it.
        /// </summary>
        /// <param name="message">A message to display. This message can be seen in the unit test results.</param>
        /// <param name="parameters">An array of parameters to use when formatting message.</param>
        public static void Inconclusive(string message, params object[] parameters)
        {
            throw new NotSupportedException(String.Format(message, parameters));
        }

        /// <summary>
        /// Verifies that the specified condition is false. The assertion fails if the condition is true.
        /// </summary>
        /// <param name="condition">The condition to verify is false.</param>
        public static void IsFalse(bool condition)
        {
            Xunit.Assert.False(condition);
        }

        /// <summary>
        /// Verifies that the specified condition is false. The assertion fails if the condition is true. 
        /// Displays a message if the assertion fails.
        /// </summary>
        /// <param name="condition">The condition to verify is false.</param>
        /// <param name="message">A message to display if the assertion fails. This message can be seen in the unit test results.</param>
        public static void IsFalse(bool condition, string message)
        {
            Xunit.Assert.False(condition, message);
        }

        /// <summary>
        /// Verifies that the specified condition is false. The assertion fails if the condition is true. 
        /// Displays a message if the assertion fails, and applies the specified formatting to it.
        /// </summary>
        /// <param name="condition">The condition to verify is false.</param>
        /// <param name="message">A message to display if the assertion fails. This message can be seen in the unit test results.</param>
        /// <param name="parameters">An array of parameters to use when formatting message.</param>
        public static void IsFalse(bool condition, string message, params object[] parameters)
        {
            Xunit.Assert.False(condition, String.Format(message, parameters));
        }

        /// <summary>
        /// Verifies that the specified object is an instance of the specified type. 
        /// The assertion fails if the type is not found in the inheritance hierarchy of the object.
        /// </summary>
        /// <param name="value">The object to verify is of expectedType.</param>
        /// <param name="expectedType">The type expected to be found in the inheritance hierarchy of value.</param>
        public static void IsInstanceOfType(object value, Type expectedType)
        {
            Xunit.Assert.IsAssignableFrom(expectedType, value);
        }

        /// <summary>
        /// Verifies that the specified object is an instance of the specified type. 
        /// The assertion fails if the type is not found in the inheritance hierarchy of the object. 
        /// Displays a message if the assertion fails.
        /// </summary>
        /// <param name="value">The object to verify is of expectedType.</param>
        /// <param name="expectedType">The type expected to be found in the inheritance hierarchy of value.</param>
        /// <param name="message">A message to display if the assertion fails. This message can be seen in the unit test results.</param>
        public static void IsInstanceOfType(object value, Type expectedType, string message)
        {
            Xunit.Assert.IsAssignableFrom(expectedType, value);
        }

        /// <summary>
        /// Verifies that the specified object is an instance of the specified type. 
        /// The assertion fails if the type is not found in the inheritance hierarchy of the object. 
        /// Displays a message if the assertion fails, and applies the specified formatting to it.
        /// </summary>
        /// <param name="value">The object to verify is of expectedType.</param>
        /// <param name="expectedType">The type expected to be found in the inheritance hierarchy of value.</param>
        /// <param name="message">A message to display if the assertion fails. This message can be seen in the unit test results.</param>
        /// <param name="parameters">An array of parameters to use when formatting message.</param>
        public static void IsInstanceOfType(object value, Type expectedType, string message, params object[] parameters)
        {
            Xunit.Assert.IsAssignableFrom(expectedType, value);
        }

        /// <summary>
        /// Verifies that the specified object is not an instance of the specified type. 
        /// The assertion fails if the type is found in the inheritance hierarchy of the object.
        /// </summary>
        /// <param name="value">The object to verify is not of wrongType.</param>
        /// <param name="wrongType">The type that should not be found in the inheritance hierarchy of value.</param>
        public static void IsNotInstanceOfType(object value, Type wrongType)
        {
            //Xunit.Assert.IsNotType(wrongType, value);

            //Note, maybe wrong, may use the following:
            try
            {
                Xunit.Assert.IsAssignableFrom(wrongType, value);
                Xunit.Assert.True(false);
            }
            catch
            {

            }
        }

        /// <summary>
        /// Verifies that the specified object is not an instance of the specified type. 
        /// The assertion fails if the type is found in the inheritance hierarchy of the object. 
        /// Displays a message if the assertion fails.
        /// </summary>
        /// <param name="value">The object to verify is not of wrongType.</param>
        /// <param name="wrongType">The type that should not be found in the inheritance hierarchy of value.</param>
        /// <param name="message">A message to display if the assertion fails. This message can be seen in the unit test results.</param>
        public static void IsNotInstanceOfType(object value, Type wrongType, string message)
        {
            IsNotInstanceOfType(value, wrongType);
        }

        /// <summary>
        /// Verifies that the specified object is not an instance of the specified type. 
        /// The assertion fails if the type is found in the inheritance hierarchy of the object. 
        /// Displays a message if the assertion fails, and applies the specified formatting to it.
        /// </summary>
        /// <param name="value">The object to verify is not of wrongType.</param>
        /// <param name="wrongType">The type that should not be found in the inheritance hierarchy of value.</param>
        /// <param name="message">A message to display if the assertion fails. This message can be seen in the unit test results.</param>
        /// <param name="parameters">An array of parameters to use when formatting message.</param>
        public static void IsNotInstanceOfType(object value, Type wrongType, string message, params object[] parameters)
        {
            IsNotInstanceOfType(value, wrongType);
        }

        /// <summary>
        /// Verifies that the specified object is not null. The assertion fails if it is null.
        /// </summary>
        /// <param name="value">The object to verify is not null.</param>
        public static void IsNotNull(object value)
        {
            Xunit.Assert.NotNull(value);
        }

        /// <summary>
        /// Verifies that the specified object is not null. The assertion fails if it is null. 
        /// Displays a message if the assertion fails.
        /// </summary>
        /// <param name="value">The object to verify is not null.</param>
        /// <param name="message">A message to display if the assertion fails. This message can be seen in the unit test results.</param>
        public static void IsNotNull(object value, string message)
        {
            Xunit.Assert.NotNull(value);
        }

        /// <summary>
        /// Verifies that the specified object is not null. The assertion fails if it is null. 
        /// Displays a message if the assertion fails, and applies the specified formatting to it.
        /// </summary>
        /// <param name="value">The object to verify is not null.</param>
        /// <param name="message">A message to display if the assertion fails. This message can be seen in the unit test results.</param>
        /// <param name="parameters">An array of parameters to use when formatting message.</param>
        public static void IsNotNull(object value, string message, params object[] parameters)
        {
            Xunit.Assert.NotNull(value);
        }

        /// <summary>
        /// Verifies that the specified object is null. The assertion fails if it is not null.
        /// </summary>
        /// <param name="value">The object to verify is null.</param>
        public static void IsNull(object value)
        {
            Xunit.Assert.Null(value);
        }

        /// <summary>
        /// Verifies that the specified object is null. The assertion fails if it is not null. 
        /// Displays a message if the assertion fails.
        /// </summary>
        /// <param name="value">The object to verify is null.</param>
        /// <param name="message">A message to display if the assertion fails. This message can be seen in the unit test results.</param>
        public static void IsNull(object value, string message)
        {
            Xunit.Assert.Null(value);
        }

        /// <summary>
        /// Verifies that the specified object is null. The assertion fails if it is not null. 
        /// Displays a message if the assertion fails, and applies the specified formatting to it.
        /// </summary>
        /// <param name="value">The object to verify is null.</param>
        /// <param name="message">A message to display if the assertion fails. This message can be seen in the unit test results.</param>
        /// <param name="parameters">An array of parameters to use when formatting message.</param>
        public static void IsNull(object value, string message, params object[] parameters)
        {
            Xunit.Assert.Null(value);
        }

        /// <summary>
        /// Verifies that the specified condition is true. The assertion fails if the condition is false.
        /// </summary>
        /// <param name="condition">The condition to verify is true.</param>
        public static void IsTrue(bool condition)
        {
            Xunit.Assert.True(condition);
        }

        /// <summary>
        /// Verifies that the specified condition is true. The assertion fails if the condition is false. 
        /// Displays a message if the assertion fails.
        /// </summary>
        /// <param name="condition">The condition to verify is true.</param>
        /// <param name="message">A message to display if the assertion fails. This message can be seen in the unit test results.</param>
        public static void IsTrue(bool condition, string message)
        {
            Xunit.Assert.True(condition, message);
        }

        /// <summary>
        /// Verifies that the specified condition is true. The assertion fails if the condition is false. 
        /// Displays a message if the assertion fails, and applies the specified formatting to it.
        /// </summary>
        /// <param name="condition">The condition to verify is true.</param>
        /// <param name="message">A message to display if the assertion fails. This message can be seen in the unit test results.</param>
        /// <param name="parameters">An array of parameters to use when formatting message.</param>
        public static void IsTrue(bool condition, string message, params object[] parameters)
        {
            Xunit.Assert.True(condition, String.Format(message, parameters));
        }

        /// <summary>
        /// Verifies if exception is a particular exeption is thrown
        /// </summary>
        /// <typeparam name="TException">Exception type</typeparam>
        /// <param name="action">action to perform</param>
        /// <param name="message">fail message if no exception thrown</param>
        /// <param name="allowDerivedTypes">if allow derived types when checking exception</param>
        public static void ThrowsException<TException>(Action action, string message, bool allowDerivedTypes = true)
        {
            try
            {
                action();
                Fail(message);
            }
            catch (Exception ex)
            {
                if (allowDerivedTypes && !(ex is TException))
                    Fail("Delegate throws exception of type " + ex.GetType().Name + ", but " + typeof(TException).Name + " or a derived type was expected.");
                if (!allowDerivedTypes && ex.GetType() != typeof(TException))
                    Fail("Delegate throws exception of type " + ex.GetType().Name + ", but " + typeof(TException).Name + " was expected.");
            }
        }
    }
}