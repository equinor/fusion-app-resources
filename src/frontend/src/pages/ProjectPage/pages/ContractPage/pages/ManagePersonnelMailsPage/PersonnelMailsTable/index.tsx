import { FC, useMemo } from 'react';
import { FusionTable } from '@equinor/fusion-react-table';
import Personnel from '../../../../../../../models/Personnel';
import getColumns from './getColumns';
type PersonnelMailsTableProps = {
    isFetching: boolean;
    personnel: Personnel[];
};
const PersonnelMailsTable: FC<PersonnelMailsTableProps> = ({ isFetching, personnel }) => {
    const columns = useMemo(() => getColumns(isFetching), [isFetching]);

    return <FusionTable data={personnel} columns={columns} spacing="xx_small" />;
};

export default PersonnelMailsTable;
