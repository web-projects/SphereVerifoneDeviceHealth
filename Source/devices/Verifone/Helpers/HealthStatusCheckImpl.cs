using System;
using static System.ExtensionMethods;

namespace Devices.Verifone.Helpers
{
    public static class HealthStatusCheckImpl
    {
        public const string Device24HourReboot = "07:00:00";

        public enum HealthStatusValidationRequired
        {
            [StringValue("NOTREQUIRED")]
            NOTREQUIRED,
            [StringValue("ADETESTKEY")]
            ADETESTKEY,
            [StringValue("DEBITPINKEY")]
            DEBITPINKEY,
        }

        public static HealthStatusValidationRequired ValueIsRequired(string value)
        {
            foreach (HealthStatusValidationRequired required in Enum.GetValues(typeof(HealthStatusValidationRequired)))
            {
                if (required.GetStringValue().Equals(value))
                {
                    return required;
                }
            }

            return HealthStatusValidationRequired.NOTREQUIRED;
        }
    }
}
