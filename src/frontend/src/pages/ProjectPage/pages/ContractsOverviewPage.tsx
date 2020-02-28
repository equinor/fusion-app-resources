import * as React from 'react';
import { useCurrentContext } from '@equinor/fusion';
import { DataTable } from '@equinor/fusion-components';
import * as styles from './styles.less';
import useContracts from './hooks/useContracts';
import createColumns from './Columns';

const ContractsOverviewPage = () => {
    const currentProject = useCurrentContext();
    const { contracts, isFetchingContracts, contractsError } = useContracts(currentProject?.id);

    const columns = React.useMemo(() => createColumns(), []);

    return (
        <div className={styles.container}>
            <DataTable
                rowIdentifier="contractNumber"
                data={contracts}
                isFetching={isFetchingContracts}
                columns={columns}
            />
        </div>
    );
};

export default ContractsOverviewPage;
