type ProvisioningState = 'NotProvisioned' | 'Provisioned' | 'Error' | 'Unknown';

type ProvisioningStatus = {
    state: ProvisioningState;
    positionId: string | null;
    provisioned: Date | null;
    errorMessage: string;
    errorPayload: string;
};

export default ProvisioningStatus;
