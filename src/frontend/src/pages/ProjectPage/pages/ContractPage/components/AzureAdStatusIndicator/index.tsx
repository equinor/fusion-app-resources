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
};

type AzureAdStatusIndicatorProps = {
    status: azureAdStatus;
};

const AzureAdStatusIndicator: FC<AzureAdStatusIndicatorProps> = ({ status }) => {
    const { text, color, id } = AdStatus[status];
    return <div id={id} ref={useTooltipRef(text)}>{<AdStatusIcon color={color} />}</div>;
};

export const AzureAdStatusTextFormat = (status: azureAdStatus) => {
    return AdStatus[status].text;
};

export const AzureAdStatusColor = (status: azureAdStatus) => {
    return AdStatus[status].color;
};

export const AzureAdStatusId = (status: azureAdStatus) => {
    return AdStatus[status].id;
};

export default AzureAdStatusIndicator;
