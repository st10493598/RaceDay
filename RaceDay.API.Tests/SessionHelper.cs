using Microsoft.AspNetCore.Http;
using Moq;
using System;
using System.Collections.Generic;

namespace RaceDay.API.Tests
{
    public static class SessionHelper
    {
        public static ISession CreateSession(int? userId, string? role)
        {
            var sessionMock = new Mock<ISession>();

            var sessionValues = new Dictionary<string, byte[]>();

            if (userId.HasValue)
            {
                sessionValues["UserID"] = BitConverter.GetBytes(userId.Value);
            }

            if (role != null)
            {
                sessionValues["Role"] = System.Text.Encoding.UTF8.GetBytes(role);
            }

            sessionMock.Setup(s => s.TryGetValue(
                It.IsAny<string>(),
                out It.Ref<byte[]>.IsAny))
                .Returns((string key, out byte[] value) =>
                {
                    if (sessionValues.TryGetValue(key, out var storedValue))
                    {
                        value = storedValue;
                        return true;
                    }

                    value = null!;
                    return false;
                });

            sessionMock.Setup(s => s.Set(
                It.IsAny<string>(),
                It.IsAny<byte[]>()))
                .Callback<string, byte[]>((key, value) =>
                {
                    sessionValues[key] = value;
                });

            return sessionMock.Object;
        }
    }
}
