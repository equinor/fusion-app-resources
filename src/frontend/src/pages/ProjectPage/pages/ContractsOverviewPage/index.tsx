import * as React from 'react';
import { useCurrentContext, combineUrls } from '@equinor/fusion';
import { DataTable, Button } from '@equinor/fusion-components';
import * as styles from './styles.less';
import createColumns from './Columns';
import useContracts from './hooks/useContracts';
import GenericFilter from '../ContractPage/pages/components/GenericFilter';
import getFilterSections from './getFilterSections';
import Contract from '../../../../models/contract';

const ContractsOverviewPage = () => {
    const currentProject = useCurrentContext();
    const { contracts, isFetchingContracts, contractsError } = useContracts(currentProject?.id);
    const [filteredContracts, setFilteredContracts] = React.useState<Contract[]>(contracts);

    React.useEffect(() => {
        setFilteredContracts(contracts);
    }, [contracts]);

    const columns = React.useMemo(() => createColumns(), []);
    const filterSections = React.useMemo(() => getFilterSections(contracts), [contracts]);

    return (
        <div className={styles.container}>
            <div className={styles.tableContainer}>
                <Button relativeUrl={combineUrls(currentProject?.id || '', 'allocate')}>
                    Allocate contract
                </Button>
                <DataTable
                    rowIdentifier="contractNumber"
                    data={filteredContracts}
                    isFetching={isFetchingContracts}
                    columns={columns}
                />
            </div>
            <GenericFilter
                data={contracts}
                filterSections={filterSections}
                onFilter={setFilteredContracts}
            />
        </div>
    );
};

export default ContractsOverviewPage;
