using Servicer.Core.CardType;
using Servicer.Core.EMVKernel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using XO.Enums;

namespace Servicer.Core.Action.Payment
{
    public class PaymentActionProcessor
    {
        private string EMVKernelVersion;
        private string ContactlessKernelInformation;
        private List<AidKernelVersions> aidKernelVersions = new List<AidKernelVersions>();

        public PaymentActionProcessor(string eMVKernelVersion, string clessKernelVersion)
        {
            EMVKernelVersion = eMVKernelVersion;
            ContactlessKernelInformation = clessKernelVersion;

            // Map to EMV Kernel Versions
            string[] kernelRevisions = ContactlessKernelInformation.Split(';');
            foreach (string kernelVersion in kernelRevisions)
            {
                Debug.WriteLine($"EMV KERNEL VERSION: \"{kernelVersion}\"");
                SetContactlessEMVKernelVersion(kernelVersion.Substring(0, 2), kernelVersion.Substring(2));
            }
        }

        private string AddAidKernelVersion(string aid, string kernelVersion)
        {
            aidKernelVersions.Add(new AidKernelVersions(aid, kernelVersion));
            return string.Empty;
        }

        private string SetContactlessEMVKernelVersion(string kernelIdentifier, string kernelVersion) => kernelIdentifier switch
        {
            "AK" => AddAidKernelVersion(AidList.FirstDataRapidConnectAIDList.FirstOrDefault(x => x.CardBrand == TenderType.AMEX).AIDValue, kernelVersion),
            "DK" => AddAidKernelVersion(AidList.FirstDataRapidConnectAIDList.FirstOrDefault(x => x.CardBrand == TenderType.DinersClub).AIDValue, kernelVersion),
            "IK" => AddAidKernelVersion(AidList.FirstDataRapidConnectAIDList.FirstOrDefault(x => x.CardBrand == TenderType.Interac).AIDValue, kernelVersion),
            "JK" => AddAidKernelVersion(AidList.FirstDataRapidConnectAIDList.FirstOrDefault(x => x.CardBrand == TenderType.JCB).AIDValue, kernelVersion),
            "MK" => AddAidKernelVersion(AidList.FirstDataRapidConnectAIDList.FirstOrDefault(x => x.CardBrand == TenderType.MasterCard).AIDValue, kernelVersion),
            "VK" => AddAidKernelVersion(AidList.FirstDataRapidConnectAIDList.FirstOrDefault(x => x.CardBrand == TenderType.Visa).AIDValue, kernelVersion),
            // UNMAPPED AIDS
            "CK" => AddAidKernelVersion(kernelIdentifier, kernelVersion),
            "EK" => AddAidKernelVersion(kernelIdentifier, kernelVersion),
            "EP" => AddAidKernelVersion(kernelIdentifier, kernelVersion),
            "GK" => AddAidKernelVersion(kernelIdentifier, kernelVersion),
            "MR" => AddAidKernelVersion(kernelIdentifier, kernelVersion),
            "PB" => AddAidKernelVersion(kernelIdentifier, kernelVersion),
            "PK" => AddAidKernelVersion(kernelIdentifier, kernelVersion),
            "RK" => AddAidKernelVersion(kernelIdentifier, kernelVersion),

            _ => throw new Exception($"Invalid EMV Kernel Version: '{kernelVersion}'.")
        };

        private AidEntry GetPaymentType(string paymentAID)
        {
            if (string.IsNullOrWhiteSpace(paymentAID))
            {
                return null;
            }
            //TODO: currently, only processor is FDRC - need to enhance to others
            AidEntry targetAid = AidList.FirstDataRapidConnectAIDList.FirstOrDefault(x => x.AIDValue.Equals(paymentAID, StringComparison.OrdinalIgnoreCase));
            return targetAid;
        }

        public string GetPaymentContactlessEMVKernelVersion(string applicationIdentifier)
        {
            //AidEntry aidType = GetPaymentType(applicationIdentifier);
            return aidKernelVersions.FirstOrDefault(x => x.AidValue == applicationIdentifier)?.KernelVersion ?? "NOT FOUND";
        }
    }
}
