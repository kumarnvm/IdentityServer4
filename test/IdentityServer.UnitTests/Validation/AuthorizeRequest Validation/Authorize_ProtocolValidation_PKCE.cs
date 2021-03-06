﻿// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using FluentAssertions;
using IdentityModel;
using IdentityServer4.Configuration;
using IdentityServer4.UnitTests.Common;
using System.Collections.Specialized;
using System.Threading.Tasks;
using Xunit;

namespace IdentityServer4.UnitTests.Validation.AuthorizeRequest
{
    public class Authorize_ProtocolValidation_Valid_PKCE
    {
        const string Category = "AuthorizeRequest Protocol Validation - PKCE";

        InputLengthRestrictions lengths = new InputLengthRestrictions();

        [Fact]
        [Trait("Category", Category)]
        public async Task valid_openid_code_request_with_challenge_and_plain_method_should_be_allowed()
        {
            var parameters = new NameValueCollection();
            parameters.Add(OidcConstants.AuthorizeRequest.ClientId, "codeclient.pkce");
            parameters.Add(OidcConstants.AuthorizeRequest.Scope, "openid");
            parameters.Add(OidcConstants.AuthorizeRequest.CodeChallenge, "x".Repeat(lengths.CodeChallengeMinLength));
            parameters.Add(OidcConstants.AuthorizeRequest.CodeChallengeMethod, OidcConstants.CodeChallengeMethods.Plain);
            parameters.Add(OidcConstants.AuthorizeRequest.RedirectUri, "https://server/cb");
            parameters.Add(OidcConstants.AuthorizeRequest.ResponseType, OidcConstants.ResponseTypes.Code);

            var validator = Factory.CreateAuthorizeRequestValidator();
            var result = await validator.ValidateAsync(parameters);

            result.IsError.Should().Be(false);
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task valid_openid_code_request_with_challenge_and_sh256_method_should_be_allowed()
        {
            var parameters = new NameValueCollection();
            parameters.Add(OidcConstants.AuthorizeRequest.ClientId, "codeclient.pkce");
            parameters.Add(OidcConstants.AuthorizeRequest.Scope, "openid");
            parameters.Add(OidcConstants.AuthorizeRequest.CodeChallenge, "x".Repeat(lengths.CodeChallengeMinLength));
            parameters.Add(OidcConstants.AuthorizeRequest.CodeChallengeMethod, OidcConstants.CodeChallengeMethods.Sha256);
            parameters.Add(OidcConstants.AuthorizeRequest.RedirectUri, "https://server/cb");
            parameters.Add(OidcConstants.AuthorizeRequest.ResponseType, OidcConstants.ResponseTypes.Code);

            var validator = Factory.CreateAuthorizeRequestValidator();
            var result = await validator.ValidateAsync(parameters);

            result.IsError.Should().Be(false);
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task valid_openid_code_request_with_challenge_and_missing_method_should_be_allowed()
        {
            var parameters = new NameValueCollection();
            parameters.Add(OidcConstants.AuthorizeRequest.ClientId, "codeclient.pkce");
            parameters.Add(OidcConstants.AuthorizeRequest.Scope, "openid");
            parameters.Add(OidcConstants.AuthorizeRequest.CodeChallenge, "x".Repeat(lengths.CodeChallengeMinLength));
            parameters.Add(OidcConstants.AuthorizeRequest.RedirectUri, "https://server/cb");
            parameters.Add(OidcConstants.AuthorizeRequest.ResponseType, OidcConstants.ResponseTypes.Code);

            var validator = Factory.CreateAuthorizeRequestValidator();
            var result = await validator.ValidateAsync(parameters);

            result.IsError.Should().Be(false);
        }


        [Fact]
        [Trait("Category", Category)]
        public async Task openid_code_request_missing_challenge_should_be_rejected()
        {
            var parameters = new NameValueCollection();
            parameters.Add(OidcConstants.AuthorizeRequest.ClientId, "codeclient.pkce");
            parameters.Add(OidcConstants.AuthorizeRequest.Scope, "openid");
            parameters.Add(OidcConstants.AuthorizeRequest.RedirectUri, "https://server/cb");
            parameters.Add(OidcConstants.AuthorizeRequest.ResponseType, OidcConstants.ResponseTypes.Code);

            var validator = Factory.CreateAuthorizeRequestValidator();
            var result = await validator.ValidateAsync(parameters);

            result.IsError.Should().Be(true);
            result.Error.Should().Be(OidcConstants.AuthorizeErrors.InvalidRequest);
            result.ErrorDescription.Should().Be("code challenge required");
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task openid_hybrid_request_missing_challenge_should_be_rejected()
        {
            var parameters = new NameValueCollection();
            parameters.Add(OidcConstants.AuthorizeRequest.ClientId, "hybridclient.pkce");
            parameters.Add(OidcConstants.AuthorizeRequest.Scope, "openid");
            parameters.Add(OidcConstants.AuthorizeRequest.RedirectUri, "https://server/cb");
            parameters.Add(OidcConstants.AuthorizeRequest.ResponseType, OidcConstants.ResponseTypes.CodeIdToken);

            var validator = Factory.CreateAuthorizeRequestValidator();
            var result = await validator.ValidateAsync(parameters);

            result.IsError.Should().Be(true);
            result.Error.Should().Be(OidcConstants.AuthorizeErrors.InvalidRequest);
            result.ErrorDescription.Should().Be("code challenge required");
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task openid_code_request_with_challenge_and_invalid_method_should_be_rejected()
        {
            var parameters = new NameValueCollection();
            parameters.Add(OidcConstants.AuthorizeRequest.ClientId, "codeclient.pkce");
            parameters.Add(OidcConstants.AuthorizeRequest.Scope, "openid");
            parameters.Add(OidcConstants.AuthorizeRequest.CodeChallenge, "x".Repeat(lengths.CodeChallengeMinLength));
            parameters.Add(OidcConstants.AuthorizeRequest.CodeChallengeMethod, "invalid");
            parameters.Add(OidcConstants.AuthorizeRequest.RedirectUri, "https://server/cb");
            parameters.Add(OidcConstants.AuthorizeRequest.ResponseType, OidcConstants.ResponseTypes.Code);

            var validator = Factory.CreateAuthorizeRequestValidator();
            var result = await validator.ValidateAsync(parameters);

            result.IsError.Should().Be(true);
            result.Error.Should().Be(OidcConstants.AuthorizeErrors.InvalidRequest);
            result.ErrorDescription.Should().Be("transform algorithm not supported");
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task openid_code_request_with_too_short_challenge_should_be_rejected()
        {
            var parameters = new NameValueCollection();
            parameters.Add(OidcConstants.AuthorizeRequest.ClientId, "codeclient.pkce");
            parameters.Add(OidcConstants.AuthorizeRequest.Scope, "openid");
            parameters.Add(OidcConstants.AuthorizeRequest.CodeChallenge, "x".Repeat(lengths.CodeChallengeMinLength - 1));
            parameters.Add(OidcConstants.AuthorizeRequest.CodeChallengeMethod, OidcConstants.CodeChallengeMethods.Plain);
            parameters.Add(OidcConstants.AuthorizeRequest.RedirectUri, "https://server/cb");
            parameters.Add(OidcConstants.AuthorizeRequest.ResponseType, OidcConstants.ResponseTypes.Code);

            var validator = Factory.CreateAuthorizeRequestValidator();
            var result = await validator.ValidateAsync(parameters);

            result.IsError.Should().Be(true);
            result.Error.Should().Be(OidcConstants.AuthorizeErrors.InvalidRequest);
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task openid_code_request_with_too_long_challenge_should_be_rejected()
        {
            var parameters = new NameValueCollection();
            parameters.Add(OidcConstants.AuthorizeRequest.ClientId, "codeclient.pkce");
            parameters.Add(OidcConstants.AuthorizeRequest.Scope, "openid");
            parameters.Add(OidcConstants.AuthorizeRequest.CodeChallenge, "x".Repeat(lengths.CodeChallengeMaxLength + 1));
            parameters.Add(OidcConstants.AuthorizeRequest.CodeChallengeMethod, OidcConstants.CodeChallengeMethods.Plain);
            parameters.Add(OidcConstants.AuthorizeRequest.RedirectUri, "https://server/cb");
            parameters.Add(OidcConstants.AuthorizeRequest.ResponseType, OidcConstants.ResponseTypes.Code);

            var validator = Factory.CreateAuthorizeRequestValidator();
            var result = await validator.ValidateAsync(parameters);

            result.IsError.Should().Be(true);
            result.Error.Should().Be(OidcConstants.AuthorizeErrors.InvalidRequest);
        }
    }
}