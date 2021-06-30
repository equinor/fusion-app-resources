import { styling } from '@equinor/fusion-components';
import { FusionColumn } from '@equinor/fusion-react-table/dist/types';
import Personnel from '../../../../../../../models/Personnel';
import AzureAdStatusIndicator from '../../../components/AzureAdStatusIndicator';
import PersonCell from './cells/PersonCell';
import PreferredMail from './cells/PreferredMail';
import HasEquinorMailCell from '../../../components/HasEquinorMailCell';

const getColumns = (isFetching: boolean): FusionColumn<Personnel>[] => {
    return [
        {
            Header: 'AD',
            accessor: 'azureAdStatus',
            Cell: ({ row }) => (
                <AzureAdStatusIndicator status={row.original.azureAdStatus || 'NoAccount'} />
            ),
            maxWidth: styling.numericalGrid(2),
            width: styling.numericalGrid(2),
        },
        {
            Header: 'Equinor mail',
            accessor: 'mail',
            Cell: ({ row }) => <HasEquinorMailCell item={row.original} />,
            maxWidth: styling.numericalGrid(2),
            width: styling.numericalGrid(2),
        },
        {
            Header: 'Person',
            accessor: 'azureUniquePersonId',

            Cell: ({ row }) => <PersonCell item={row.original} />,
        },

        {
            Header: 'Preferred mail',
            accessor: 'lastName',
            Cell: ({ row }) => <PreferredMail item={row.original} />,
        },
    ];
};

export default getColumns;
