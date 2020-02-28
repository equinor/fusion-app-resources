import * as React from 'react';
import { useCurrentContext } from '@equinor/fusion';
import { DataTable } from '@equinor/fusion-components';
import * as styles from './styles.less';
import createColumns from './Columns';
import useContracts from './hooks/useContracts';

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
