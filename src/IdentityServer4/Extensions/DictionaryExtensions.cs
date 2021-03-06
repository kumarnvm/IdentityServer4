﻿using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace IdentityServer4.Extensions
{
    public static class DictionaryExtensions
    {
        public static void AddDictionary(this JObject jobject, Dictionary<string, object> dictionary)
        {
            foreach (var item in dictionary)
            {
                JToken token;
                if (jobject.TryGetValue(item.Key, out token))
                {
                    throw new Exception("Item does already exist - cannot add it via a custom entry: " + item.Key);
                }

                if (item.Value.GetType().GetTypeInfo().IsClass)
                {
                    jobject.Add(new JProperty(item.Key, JToken.FromObject(item.Value)));
                }
                else
                {
                    jobject.Add(new JProperty(item.Key, item.Value));
                }
            }
        }
    }
}