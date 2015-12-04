/*
 * Copyright 2010-2014 Amazon.com, Inc. or its affiliates. All Rights Reserved.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License").
 * You may not use this file except in compliance with the License.
 * A copy of the License is located at
 * 
 *  http://aws.amazon.com/apache2.0
 * 
 * or in the "license" file accompanying this file. This file is distributed
 * on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either
 * express or implied. See the License for the specific language governing
 * permissions and limitations under the License.
 */

using System;
using System.Net;

using Amazon.Util;

namespace Amazon.SecurityToken.SAML
{
    /// <summary>
    /// Interface implemented by plugins supplied to the SAMLAuthenticationController
    /// to perform the call to the authentication endpoint. The implementor returns the
    /// final response from the authentication process for subsequent parsing.
    /// </summary>
    public interface IAuthenticationController
    {
        /// <summary>
        /// Calls the specified endpoint, optionally providing custom credentials.
        /// </summary>
        /// <param name="identityProvider">The endpoint providing </param>
        /// <param name="credentials">
        /// Optional, if not supplied the token for the currently logged-in user is supplied to the authentication endpoint.
        /// </param>
        /// <param name="authenticationType">
        /// The authentication type expected by the endpoint. Valid values are 'NTLM',
        /// 'Digest', 'Kerberos' and 'Negotiate'.
        /// </param>
        /// <returns>The raw response data from the authentication request.</returns>
        string Authenticate(Uri identityProvider, ICredentials credentials, string authenticationType);
    }

    /// <summary>
    /// Interface implemented by plugins supplied to the SAMLAuthenticationController
    /// to parse an authentication response returned by an IAuthenticator instance and
    /// yield a SAMLAssertion instance.
    /// </summary>
    public interface IAuthenticationResponseParser
    {
        /// <summary>
        /// Parses the supplied reponse data to instantiate a SAMLAssertion instance
        /// containing IAM role and token data that can be used to generate temporary
        /// AWS credentials.
        /// </summary>
        /// <param name="authenticationResponse">
        /// The response that was returned from user authentication.
        /// </param>
        /// <returns>SAMLAssertion instance corresponding to the response data.</returns>
        SAMLAssertion Parse(string authenticationResponse);
    }

    /// <summary>
    /// Helper class to perform SAML authentication negotiation for an identity 
    /// provider and relying party combination. Yields a SAMLAssertion instance 
    /// that can be used to retrieve temporary, auto-refreshing AWS credentials.
    /// </summary>
    public class SAMLAuthenticationController
    {
        /// <summary>
        /// Handler that will be called to perform the authentication process to a 
        /// defined endpoint.
        /// </summary>
        public IAuthenticationController AuthenticationController { get; private set; }

        /// <summary>
        /// Handler that will be called to parse the response from a succesful
        /// authentication request.
        /// </summary>
        public IAuthenticationResponseParser ResponseParser { get; private set; }

        /// <summary>
        /// Instantiates a controller instance configured to use the built-in AD FS
        /// classes to authenticate and parse the responses.
        /// </summary>
        public SAMLAuthenticationController()
            : this(new AdfsAuthenticationController(), new AdfsAuthenticationResponseParser())
        {
        }

        /// <summary>
        /// Instantiates the controller to use the specified instances to perform authentication
        /// and response parsing.
        /// </summary>
        /// <param name="authenticationController">Handler that will be called to perform authentication.</param>
        /// <param name="responseParser">Handler that will be called to parse successful authentication responses</param>
        public SAMLAuthenticationController(IAuthenticationController authenticationController, IAuthenticationResponseParser responseParser)
        {
            if (authenticationController == null)
                throw new ArgumentNullException("authenticationController");
            if (responseParser == null)
                throw new ArgumentNullException("responseParser");

            AuthenticationController = authenticationController;
            ResponseParser = responseParser;
        }

        /// <summary>
        /// Authenticates the specified network credentials with a provider endpoint and
        /// returns the SAML assertion data from which temporary AWS credentials can be obtained.
        /// </summary>
        /// <param name="identityProviderUrl">The authentication endpoint to be called.</param>
        /// <param name="credentials">
        /// Credentials for the call. If null, the users default network credentials will be used
        /// in a temporary impersonation context.
        /// </param>
        /// <param name="authenticationType">
        /// The authentication type expected by the endpoint. The default value if not specified
        /// is 'Kerberos'. Valid values are 'NTLM', 'Digest', 'Kerberos' and 'Negotiate'.
        /// </param>
        /// <returns></returns>
        public SAMLAssertion GetSAMLAssertion(string identityProviderUrl, ICredentials credentials, string authenticationType)
        {
            return GetSAMLAssertion(new Uri(identityProviderUrl), credentials, authenticationType);
        }

        /// <summary>
        /// Authenticates the specified network credentials with a provider endpoint and
        /// returns the SAML assertion data from which temporary AWS credentials can be obtained.
        /// </summary>
        /// <param name="identityProviderUrl">The authentication endpoint to be called.</param>
        /// <param name="credentials">
        /// Credentials for the call. If null, the users default network credentials will be used
        /// in a temporary impersonation context.
        /// </param>
        /// <param name="authenticationType">
        /// The authentication type expected by the endpoint. The default value if not specified
        /// is 'Kerberos'. Valid values are 'NTLM', 'Digest', 'Kerberos' and 'Negotiate'.
        /// </param>
        /// <returns>The SAML assertion document on successful authentication.</returns>
        public SAMLAssertion GetSAMLAssertion(Uri identityProviderUrl, ICredentials credentials, string authenticationType)
        {
            var response = AuthenticationController.Authenticate(identityProviderUrl, 
                                                                 credentials,
                                                                 string.IsNullOrEmpty(authenticationType) 
                                                                    ? SAMLEndpointSettings.DefaultAuthenticationType : authenticationType);
            return ResponseParser.Parse(response);
        }
    }
}