﻿// ----------------------------------------------------------------------------------
//
// Copyright Microsoft Corporation
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// ----------------------------------------------------------------------------------

using System.Globalization;
using System.Management.Automation;
using Microsoft.AzureStack.AzureConsistentStorage;
using Microsoft.AzureStack.AzureConsistentStorage.Models;

namespace Microsoft.AzureStack.AzureConsistentStorage.Commands
{
    /// <summary>
    ///     SYNTAX
    ///         New-ACSQuota [-SubscriptionId] {string} [-Token] {string} [-AdminUri] {Uri} [-Location] {string} 
    ///             [-SkipCertificateValidation] [-QuotaName] {string} [-NumberOfStorageAccounts] {int}  [-CapacityInGB] {int} [ {CommonParameters}] 
    /// 
    /// </summary>
    [Cmdlet(VerbsCommon.New, Nouns.AdminQuota, SupportsShouldProcess = true)]
    public sealed class NewAdminQuota : AdminCmdlet
    {
        const string ShouldProcessTargetFormat = "quota {0} ";

        /// <summary>
        /// Location
        /// </summary>
        [Parameter(Position = 3, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        [ValidateNotNull]
        public string Location { get; set; }

        /// <summary>
        ///  Quota Name
        /// </summary>
        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true, Position = 4)]
        [ValidateNotNull]
        public string Name { get; set; }

        /// <summary>
        /// Number of Storage Accounts for this quota    
        /// </summary>
        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true, Position = 5)]
        [ValidateNotNull]
        public int NumberOfStorageAccounts { get; set; }

        /// <summary>
        ///  Capacity for this quota   
        /// </summary>
        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true, Position = 6)]
        [ValidateNotNull]
        public int CapacityInGB { get; set; }

        protected override void Execute()
        {
            if (!string.IsNullOrEmpty(Name))
            {
                QuotaGetResponse response = Client.Quotas.Get(Location, Name);
                if (response != null && response.Quota != null)
                {
                    throw new AdminException(string.Format(CultureInfo.InvariantCulture, Resources.QuotaAlreadyExistsErrorMessage));
                }
            }
            if (ShouldProcess(string.Format(CultureInfo.InvariantCulture, ShouldProcessTargetFormat, Name)))
            {
                QuotaCreateOrUpdateParameters request = new QuotaCreateOrUpdateParameters
                { 
                    Properties = new Quota
                    {
                        CapacityInGB = CapacityInGB,
                        NumberOfStorageAccounts = NumberOfStorageAccounts
                    }
                };

                QuotaGetResponse response = Client.Quotas.CreateOrUpdate(Location, Name, request);

                WriteObject(new QuotaResponse(response.Quota));
            }
        }
    }
}