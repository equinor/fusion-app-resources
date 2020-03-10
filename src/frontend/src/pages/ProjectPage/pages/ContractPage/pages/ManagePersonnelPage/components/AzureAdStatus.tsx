import * as React from 'react';
import {
    useTooltipRef,
    DoneIcon,
    WarningIcon,
    CloseIcon,
    styling,
} from '@equinor/fusion-components';
import { azureAdStatus } from '../../../../../../../models/Personnel';

type AdStatus = {
    [index: string]: {
        text: string;
        color: string;
        icon: JSX.Element;
    };
};

// TODO: Get proper icons
const AdStatus: AdStatus = {
    Available: {
        text: 'Azure AD Approved',
        color: styling.colors.green,
        icon: <DoneIcon color={styling.colors.green} />,
    },
    InviteSent: {
        text: 'Azure AD pending approval',
        color: styling.colors.orange,
        icon: <WarningIcon outline color={styling.colors.orange} />,
    },
    NoAccount: {
        text: 'No Azure Access',
        color: styling.colors.red,
        icon: <CloseIcon color={styling.colors.red} />,
    },
};

const AzureAdStatusIcon: React.FC<azureAdStatus> = (status: azureAdStatus) => {
    const { text, icon } = AdStatus[status];
    return <div ref={useTooltipRef(text)}>{icon}</div>;
};

export const AzureAdStatusTextFormat = (status: azureAdStatus) => {
    return AdStatus[status].text;
};

export const AzureAdStatusColor = (status: azureAdStatus) => {
    return AdStatus[status].color;
};

export default AzureAdStatusIcon;
