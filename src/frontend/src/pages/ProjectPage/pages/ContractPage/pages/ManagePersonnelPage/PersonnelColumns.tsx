import * as React from 'react';
import {
    DataTableColumn,
    useTooltipRef,
    DoneIcon,
    WarningIcon,
    CloseIcon,
    styling,
} from '@equinor/fusion-components';
import Personnel from '../../../../../../models/Personnel';

export type DataItemProps = {
    item: Personnel;
};

// TODO: Get proper icons
const AdStatus = {
    Available: { text: 'Azure AD Approved', icon: <DoneIcon color={styling.colors.green} /> },
    InviteSent: {
        text: 'Azure AD pending approval',
        icon: <WarningIcon outline color={styling.colors.orange} />,
    },
    NoAccount: { text: 'No Azure Access', icon: <CloseIcon color={styling.colors.red} /> },
};

const AzureAdStatus: React.FC<DataItemProps> = ({ item }) => {
    const { text, icon } = AdStatus[item.azureAdStatus];
    return <div ref={useTooltipRef(text)}>{icon}</div>;
};

const PersonnelColumns = (): DataTableColumn<Personnel>[] => [
    {
        key: 'Name',
        accessor: 'name',
        label: 'Person',
        priority: 1,
        sortable: true,
    },
    {
        key: 'Mail',
        accessor: 'mail',
        label: 'E-Mail',
        priority: 5,
        sortable: true,
    },
    {
        key: 'azureAdStatus',
        accessor: 'azureAdStatus',
        label: 'AD',
        priority: 15,
        component: AzureAdStatus,
        sortable: true,
        width: '20px',
    },
    {
        key: 'Phone',
        accessor: 'phoneNumber',
        label: 'Phone Number',
        priority: 10,
        sortable: true,
        width: '50px',
    },
    {
        key: 'Workload',
        accessor: r => '???',
        label: 'Workload',
        priority: 20,
        sortable: true,
        width: '20px',
    },
    {
        key: 'positions',
        accessor: r => '???',
        label: 'Positions',
        priority: 25,
        sortable: true,
        width: '20px',
    },
];

export default PersonnelColumns;
