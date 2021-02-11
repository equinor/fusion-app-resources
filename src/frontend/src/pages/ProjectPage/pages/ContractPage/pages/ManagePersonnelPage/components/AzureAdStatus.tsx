
import { useTooltipRef, styling } from '@equinor/fusion-components';
import { azureAdStatus } from '../../../../../../../models/Personnel';
import AdStatusIcon from '../../../../../../../components/AdStatusIcon';
import { FC } from 'react';

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
};

const AzureAdStatusIcon: FC<azureAdStatus> = (status: azureAdStatus) => {
    const { text, color } = AdStatus[status];
    return <div ref={useTooltipRef(text)}>{<AdStatusIcon color={color} />}</div>;
};

export const AzureAdStatusTextFormat = (status: azureAdStatus) => {
    return AdStatus[status].text;
};

export const AzureAdStatusColor = (status: azureAdStatus) => {
    return AdStatus[status].color;
};

export default AzureAdStatusIcon;
