﻿// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using IdentityServer4.Validation;
using System.Threading.Tasks;
using IdentityServer4.Models;
using System.Collections.Generic;

namespace IdentityServer4.IntegrationTests.Clients
{
    public class CustomResponseExtensionGrantValidator : IExtensionGrantValidator
    {
        public Task ValidateAsync(ExtensionGrantValidationContext context)
        {
            var response = new Dictionary<string, object>
            {
                { "string_value", "some_string" },
                { "int_value", 42 },
                { "dto",  CustomResponseDto.Create }
            };

            var credential = context.Request.Raw.Get("outcome");

            if (credential == "succeed")
            {
                context.Result = new GrantValidationResult("bob", "custom", customResponse: response);
            }
            else
            {
                context.Result = new GrantValidationResult(TokenErrors.InvalidGrant, "invalid_credential", response);
            }

            return Task.CompletedTask;
        }

        public string GrantType
        {
            get { return "custom"; }
        }
    }
}