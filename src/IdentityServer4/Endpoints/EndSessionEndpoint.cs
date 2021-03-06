﻿// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using IdentityServer4.Endpoints.Results;
using IdentityServer4.Extensions;
using IdentityServer4.Hosting;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Threading.Tasks;
using System;
using System.Collections.Specialized;
using IdentityServer4.Validation;
using IdentityServer4.Models;
using System.Linq;
using IdentityModel;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using IdentityServer4.Stores;
using IdentityServer4.Configuration;

namespace IdentityServer4.Endpoints
{
    class EndSessionEndpoint : IEndpoint
    {
        private readonly ILogger<EndSessionEndpoint> _logger;
        private readonly IHttpContextAccessor _context;
        private readonly IEndSessionRequestValidator _endSessionRequestValidator;
        private readonly ClientListCookie _clientListCookie;
        private readonly IMessageStore<LogoutMessage> _logoutMessageStore;
        private readonly SessionCookie _sessionCookie;
        private readonly IClientStore _clientStore;
        private readonly IdentityServerOptions _options;

        public EndSessionEndpoint(
            ILogger<EndSessionEndpoint> logger, 
            IdentityServerOptions options,
            IHttpContextAccessor context,
            IEndSessionRequestValidator endSessionRequestValidator,
            IMessageStore<LogoutMessage> logoutMessageStore,
            SessionCookie sessionCookie,
            ClientListCookie clientListCookie,
            IClientStore clientStore)
        {
            _logger = logger;
            _options = options;
            _context = context;
            _endSessionRequestValidator = endSessionRequestValidator;
            _logoutMessageStore = logoutMessageStore;
            _sessionCookie = sessionCookie;
            _clientListCookie = clientListCookie;
            _clientStore = clientStore;
        }

        public async Task<IEndpointResult> ProcessAsync(HttpContext context)
        {
            if (context.Request.Path == Constants.ProtocolRoutePaths.EndSession.EnsureLeadingSlash())
            {
                return await ProcessSignoutAsync(context);
            }

            if (context.Request.Path == Constants.ProtocolRoutePaths.EndSessionCallback.EnsureLeadingSlash())
            {
                return await ProcessSignoutCallbackAsync(context);
            }

            return new StatusCodeResult(HttpStatusCode.NotFound);
        }

        private async Task<IEndpointResult> ProcessSignoutAsync(HttpContext context)
        {
            _logger.LogInformation("Processing signout request");

            NameValueCollection parameters = null;
            if (context.Request.Method == "GET")
            {
                parameters = context.Request.Query.AsNameValueCollection();
            }
            else if (context.Request.Method == "POST")
            {
                parameters = context.Request.Form.AsNameValueCollection();
            }
            else
            {
                _logger.LogWarning("Invalid HTTP method for end session endpoint.");
                return new StatusCodeResult(HttpStatusCode.MethodNotAllowed);
            }

            var user = await _context.HttpContext.GetIdentityServerUserAsync();
            var result = await _endSessionRequestValidator.ValidateAsync(parameters, user);

            return await CreateLogoutPageRedirectAsync(result);
        }

        private async Task<IEndpointResult> CreateLogoutPageRedirectAsync(EndSessionValidationResult result)
        {
            var validatedRequest = result.IsError ? null : result.ValidatedRequest;

            if (validatedRequest != null && 
                (validatedRequest.Client != null || validatedRequest.PostLogOutUri != null))
            {
                var msg = new MessageWithId<LogoutMessage>(new LogoutMessage(validatedRequest));
                await _logoutMessageStore.WriteAsync(msg.Id, msg);
                return new LogoutPageResult(_options.UserInteractionOptions, msg.Id);
            }

            return new LogoutPageResult(_options.UserInteractionOptions);
        }

        private async Task<IEndpointResult> ProcessSignoutCallbackAsync(HttpContext context)
        {
            if (context.Request.Method != "GET")
            {
                _logger.LogWarning("Invalid HTTP method for end session endpoint.");
                return new StatusCodeResult(HttpStatusCode.MethodNotAllowed);
            }

            _logger.LogInformation("Processing singout callback request");

            await ClearSignoutMessageIdAsync(context.Request);

            var sid = ValidateSid(context.Request);
            if (sid == null)
            {
                return new StatusCodeResult(HttpStatusCode.BadRequest);
            }

            // get URLs for iframes
            var urls = await GetClientEndSessionUrlsAsync(sid);

            // relax CSP to allow those iframe origins
            //ConfigureCspResponseHeader(urls);

            // clear cookies
            ClearSessionCookies(sid);

            // get html (with iframes)
            return new EndSessionCallbackResult(urls);
        }

        private async Task ClearSignoutMessageIdAsync(HttpRequest request)
        {
            var logoutId = request.Query[_options.UserInteractionOptions.LogoutIdParameter].FirstOrDefault();
            if (logoutId != null)
            {
                await _logoutMessageStore.DeleteAsync(logoutId);
            }
        }

        private void ClearSignoutMessageId(HttpRequest request)
        {
            throw new NotImplementedException();
        }

        private string ValidateSid(HttpRequest request)
        {
            var sidCookie = _sessionCookie.GetSessionId();
            if (sidCookie != null)
            {
                //TODO: update sid to OidcConstants when idmodel released
                var sid = request.Query["sid"].FirstOrDefault();
                if (sid != null)
                {
                    if (TimeConstantComparer.IsEqual(sid, sidCookie))
                    {
                        _logger.LogDebug("sid validation successful");
                        return sid;
                    }
                    else
                    {
                        _logger.LogError("sid in query string does not match sid from cookie");
                    }

                }
                else
                {
                    _logger.LogError("No sid in query string");
                }
            }
            else
            {
                _logger.LogError("No sid in cookie");
            }

            return null;
        }

        private async Task<IEnumerable<string>> GetClientEndSessionUrlsAsync(string sid)
        {
            // read client list to get URLs for client logout endpoints
            var clientIds = _clientListCookie.GetClients();

            var urls = new List<string>();
            foreach (var clientId in clientIds)
            {
                var client = await _clientStore.FindClientByIdAsync(clientId);

                if (client != null && client.LogoutUri.IsPresent())
                {
                    var url = client.LogoutUri;

                    // add session id if required
                    if (client.LogoutSessionRequired)
                    {
                        url = url.AddQueryString(OidcConstants.EndSessionRequest.Sid, sid);
                        //TODO: update sid to OidcConstants when idmodel released
                        url = url.AddQueryString("iss", _context.HttpContext.GetIssuerUri());
                    }

                    urls.Add(url);
                }
            }

            if (urls.Any())
            {
                var msg = urls.Aggregate((x, y) => x + ", " + y);
                _logger.LogDebug("Client end session iframe URLs: {0}", msg);
            }
            else
            {
                _logger.LogDebug("No client end session iframe URLs");
            }

            return urls;
        }

        private void ClearSessionCookies(string sid)
        {
            // session id cookie
            _sessionCookie.ClearSessionId();

            // client list cookie
            _clientListCookie.Clear();
        }
    }
}