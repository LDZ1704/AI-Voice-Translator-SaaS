using System;
using System.Collections.Generic;

namespace AI_Voice_Translator_SaaS.Models
{
    public class SubscriptionPlan
    {
        public string Id { get; init; } = default!;
        public string Name { get; init; } = default!;
        public int ConversionLimit { get; init; }
        public string PriceDisplay { get; init; } = default!;
        public bool IsTrial { get; init; }
    }

    public static class SubscriptionPlans
    {
        private static readonly Dictionary<string, SubscriptionPlan> _plans =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["Trial"] = new SubscriptionPlan
                {
                    Id = "Trial",
                    Name = "Trial (Dùng thử)",
                    ConversionLimit = 5,
                    PriceDisplay = "Miễn phí",
                    IsTrial = true
                },
                ["Basic"] = new SubscriptionPlan
                {
                    Id = "Basic",
                    Name = "Basic (Cơ bản)",
                    ConversionLimit = 500,
                    PriceDisplay = "150.000₫/tháng",
                    IsTrial = false
                },
                ["Standard"] = new SubscriptionPlan
                {
                    Id = "Standard",
                    Name = "Tiêu chuẩn (Standard)",
                    ConversionLimit = 2000,
                    PriceDisplay = "500.000₫/tháng",
                    IsTrial = false
                },
                ["Premium"] = new SubscriptionPlan
                {
                    Id = "Premium",
                    Name = "Cao cấp (Premium)",
                    ConversionLimit = 5000,
                    PriceDisplay = "1.000.000₫/tháng",
                    IsTrial = false
                }
            };

        public static IReadOnlyCollection<SubscriptionPlan> GetAll() => _plans.Values;

        public static SubscriptionPlan Get(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return _plans["Trial"];
            }

            return _plans.TryGetValue(id, out var plan) ? plan : _plans["Trial"];
        }
    }
}



