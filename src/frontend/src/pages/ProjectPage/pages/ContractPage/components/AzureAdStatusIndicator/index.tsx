import { useTooltipRef, styling } from '@equinor/fusion-components';
import { FC } from 'react';
import AdStatusIcon from '../../../../../../components/AdStatusIcon';
import { azureAdStatus } from '../../../../../../models/Personnel';

type AdStatus = {
    [index: string]: {
        text: string;
        color: string;
        id: string;
    };
};

// TODO: Get proper icons
const AdStatus: AdStatus = {
    Available: {
        text: 'Azure AD Approved',
        color: styling.colors.green,
        id: 'approved',
    },
    InviteSent: {
        text: 'Azure AD pending approval',
        color: styling.colors.orange,
        id: 'invite-sent',
    },
    NoAccount: {
        text: 'No Azure Access',
        color: styling.colors.blackAlt3,
        id: 'no-access',
    },
    DeletedAccount: {
        text: 'Azure account deleted',
        color: styling.colors.secondary,
        id: 'deleted-account',
    },
};

type AzureAdStatusIndicatorProps = {
    status: azureAdStatus;
    isDeleted?: boolean
};

const AzureAdStatusIndicator: FC<AzureAdStatusIndicatorProps> = ({ status, isDeleted }) => {
    const adStatusKey = isDeleted ? "DeletedAccount" : status
    const { text, color, id } = AdStatus[adStatusKey];
    return <div id={id} data-cy="ad-column" ref={useTooltipRef(text)}>{<AdStatusIcon color={color} />}</div>;
};

export const AzureAdStatusTextFormat = (status: azureAdStatus, isDeleted?: boolean) => {
    const adStatusKey = isDeleted ? "DeletedAccount" : status
    return AdStatus[adStatusKey].text;
};

export const AzureAdStatusColor = (status: azureAdStatus, isDeleted?: boolean) => {
    const adStatusKey = isDeleted ? "DeletedAccount" : status
    return AdStatus[adStatusKey].color;
};

export const AzureAdStatusId = (status: azureAdStatus) => {
    return AdStatus[status].id;
};

export default AzureAdStatusIndicator;
