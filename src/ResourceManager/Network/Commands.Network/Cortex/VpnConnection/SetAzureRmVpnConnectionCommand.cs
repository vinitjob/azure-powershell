﻿namespace Microsoft.Azure.Commands.Network.Cortex.VpnGateway
{
    using AutoMapper;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Management.Automation;
    using System.Security;
    using Microsoft.Azure.Commands.Network.Models;
    using Microsoft.Azure.Commands.ResourceManager.Common.Tags;
    using Microsoft.Azure.Management.Network;
    using Microsoft.WindowsAzure.Commands.Common;
    using MNM = Microsoft.Azure.Management.Network.Models;
    using Microsoft.Azure.Commands.ResourceManager.Common.ArgumentCompleters;
    using System.Linq;
    using Microsoft.Azure.Management.Internal.Resources.Utilities.Models;

    [Cmdlet(VerbsCommon.Set,
        "AzureRmVpnConnection",
        DefaultParameterSetName = CortexParameterSetNames.ByVpnConnectionName,
        SupportsShouldProcess = true),
        OutputType(typeof(PSVpnConnection))]
    public class SetAzureRmVpnConnectionCommand : VpnConnectionBaseCmdlet
    {
        [Alias("ResourceName", "VpnConnectionName")]
        [Parameter(
            Mandatory = true,
            ValueFromPipelineByPropertyName = true,
            ParameterSetName = CortexParameterSetNames.ByVpnConnectionName,
            HelpMessage = "The resource name.")]
        [ValidateNotNullOrEmpty]
        public virtual string Name { get; set; }

        [Parameter(
            Mandatory = true,
            ValueFromPipelineByPropertyName = true,
            ParameterSetName = CortexParameterSetNames.ByVpnConnectionName,
            HelpMessage = "The resource group name.")]
        [ResourceGroupCompleter]
        [ValidateNotNullOrEmpty]
        public virtual string ResourceGroupName { get; set; }

        [Alias("ParentVpnGatewayName", "VpnGatewayName")]
        [Parameter(
            Mandatory = true,
            ValueFromPipelineByPropertyName = true,
            ParameterSetName = CortexParameterSetNames.ByVpnConnectionName,
            HelpMessage = "The parent resource name.")]
        [ValidateNotNullOrEmpty]
        public virtual string ParentResourceName { get; set; }

        [Alias("VpnConnection")]
        [Parameter(
            Mandatory = true,
            ValueFromPipelineByPropertyName = true,
            ParameterSetName = CortexParameterSetNames.ByVpnConnectionObject,
            HelpMessage = "The resource id of the VpnConenction object to update.")]
        [ResourceGroupCompleter]
        public string ResourceId { get; set; }

        [Parameter(
            Mandatory = false,
            ValueFromPipelineByPropertyName = true,
            HelpMessage = "The shared key required to set this connection up.")]
        [ValidateNotNullOrEmpty]
        public SecureString SharedKey { get; set; }

        [Parameter(
            Mandatory = false,
            ValueFromPipelineByPropertyName = true,
            HelpMessage = "The bandwith that needs to be handled by this connection in mbps.")]
        [ValidateNotNullOrEmpty]
        public uint ConnectionBandwidthInMbps { get; set; }

        [Parameter(
            Mandatory = false,
            ValueFromPipelineByPropertyName = true,
            HelpMessage = "The bandwith that needs to be handled by this connection in mbps.")]
        [ResourceGroupCompleter]
        [ValidateNotNullOrEmpty]
        public PSIpsecPolicy IpSecPolicy { get; set; }

        [Parameter(
            Mandatory = false,
            HelpMessage = "Enable BGP for this connection")]
        public bool? EnableBgp { get; set; }

        [Parameter(
            Mandatory = false,
            HelpMessage = "Enable rate limiting for this connection")]
        public bool? EnableRateLimiting { get; set; }

        [Parameter(
            Mandatory = false,
            HelpMessage = "Enable internet security for this connection")]
        public bool? EnableInternetSecurity { get; set; }

        [Parameter(
            Mandatory = false,
            HelpMessage = "Run cmdlet in the background")]
        public SwitchParameter AsJob { get; set; }

        [Parameter(
            Mandatory = false,
            HelpMessage = "Do not ask for confirmation if you want to overrite a resource")]
        public SwitchParameter Force { get; set; }

        public override void Execute()
        {
            base.Execute();

            if (ParameterSetName.Equals(CortexParameterSetNames.ByVpnConnectionName, StringComparison.OrdinalIgnoreCase))
            {
                this.ResourceGroupName = this.ResourceGroupName;
                this.ParentResourceName = this.ParentResourceName;
                this.Name = this.Name;
            }
            else if (ParameterSetName.Equals(ParameterSetNames.ByResourceId, StringComparison.OrdinalIgnoreCase))
            {
                var parsedResourceId = new ResourceIdentifier(this.ResourceId);
                this.ResourceGroupName = parsedResourceId.ResourceGroupName;
                this.ParentResourceName = parsedResourceId.ParentResource;
                this.Name = parsedResourceId.ResourceName;
            }

            //// Get the vpngateway object - this will throw not found if the object is not found
            PSVpnGateway parentGateway = this.GetVpnGateway(this.ResourceGroupName, this.ParentResourceName);

            if (parentGateway != null || !parentGateway.VpnConnections.Any(connection => connection.Name.Equals(this.Name, StringComparison.OrdinalIgnoreCase)))
            {
                throw new PSArgumentException("The VpnSite to modify does not exist");
            }

            var vpnConnectionToModify = parentGateway.VpnConnections.FirstOrDefault(connection => connection.Name.Equals(this.Name, StringComparison.OrdinalIgnoreCase));

            if (this.SharedKey != null)
            {
                vpnConnectionToModify.SharedKey = SecureStringExtensions.ConvertToString(this.SharedKey);
            }

            if (this.ConnectionBandwidthInMbps > 0)
            {
                vpnConnectionToModify.ConnectionBandwidhtInMbps = Convert.ToInt32(this.ConnectionBandwidthInMbps);
            }

            if (this.EnableBgp.HasValue)
            {
                vpnConnectionToModify.EnableBgp = this.EnableBgp.Value;
            }

            if (this.EnableRateLimiting.HasValue)
            {
                vpnConnectionToModify.EnableRateLimiting = this.EnableRateLimiting.Value;
            }

            if (this.EnableInternetSecurity.HasValue)
            {
                vpnConnectionToModify.EnableInternetSecurity = this.EnableInternetSecurity.Value;
            }

            if (this.IpSecPolicy != null)
            {
                vpnConnectionToModify.IpsecPolicies = new List<PSIpsecPolicy> { this.IpSecPolicy };
            }

            bool shouldProcess = this.Force.IsPresent;
            if (!shouldProcess)
            {
                shouldProcess = ShouldProcess(Name, Properties.Resources.RemoveResourceMessage);
            }

            if (shouldProcess)
            {
                // Map to the sdk object
                var vpnGatewayModel = NetworkResourceManagerProfile.Mapper.Map<MNM.VpnGateway>(parentGateway);
                this.VpnGatewayClient.CreateOrUpdate(this.ResourceGroupName, this.Name, vpnGatewayModel);

                var createdOrUpdatedVpnGateway = this.GetVpnGateway(this.ResourceGroupName, this.Name);
                WriteObject(createdOrUpdatedVpnGateway.VpnConnections.Where(connection => connection.Name.Equals(this.Name, StringComparison.OrdinalIgnoreCase)).FirstOrDefault());
            }
        }
    }
}
