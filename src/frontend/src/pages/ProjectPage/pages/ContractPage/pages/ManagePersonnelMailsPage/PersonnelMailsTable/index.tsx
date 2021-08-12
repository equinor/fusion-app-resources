import { FC, useMemo } from 'react';
import { FusionTable } from '@equinor/fusion-react-table';
import Personnel from '../../../../../../../models/Personnel';
import getColumns from './getColumns';
import { Spinner } from '@equinor/fusion-components';
type PersonnelMailsTableProps = {
    isFetching: boolean;
    personnel: Personnel[];
};
const PersonnelMailsTable: FC<PersonnelMailsTableProps> = ({ isFetching, personnel }) => {
    const columns = useMemo(() => getColumns(isFetching), [isFetching]);
    if (isFetching) {
        return <Spinner centered />;
    }
    return <FusionTable data={personnel} columns={columns} spacing="xx_small" />;
};

export default PersonnelMailsTable;
