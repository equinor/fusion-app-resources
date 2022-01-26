import { useTooltipRef, styling } from '@equinor/fusion-components';
import { FC } from 'react';
import AdStatusIcon from '../../../../../../components/AdStatusIcon';
import { azureAdStatus } from '../../../../../../models/Personnel';

type AdStatus = {
    [index: string]: {
        text: string;
        color: string;
    };
};

// TODO: Get proper icons
const AdStatus: AdStatus = {
    Available: {
        text: 'Azure AD Approved',
        color: styling.colors.green,
    },
    InviteSent: {
        text: 'Azure AD pending approval',
        color: styling.colors.orange,
    },
    NoAccount: {
        text: 'No Azure Access',
        color: styling.colors.blackAlt3,
    },
    DeletedAccount: {
        text: 'Azure account deleted',
        color: styling.colors.secondary,
    },
};

type AzureAdStatusIndicatorProps = {
    status: azureAdStatus;
    isDeleted?: boolean
};

const AzureAdStatusIndicator: FC<AzureAdStatusIndicatorProps> = ({ status, isDeleted }) => {
    const adStatusKey = isDeleted ? "DeletedAccount" : status
    const { text, color } = AdStatus[adStatusKey];
    return <div ref={useTooltipRef(text)}>{<AdStatusIcon color={color} />}</div>;
};

export const AzureAdStatusTextFormat = (status: azureAdStatus, isDeleted?: boolean) => {
    const adStatusKey = isDeleted ? "DeletedAccount" : status
    return AdStatus[adStatusKey].text;
};

export const AzureAdStatusColor = (status: azureAdStatus, isDeleted?: boolean) => {
    const adStatusKey = isDeleted ? "DeletedAccount" : status
    return AdStatus[adStatusKey].color;
};

export default AzureAdStatusIndicator;
