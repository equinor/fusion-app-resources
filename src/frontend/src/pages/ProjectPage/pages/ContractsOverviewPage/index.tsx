import * as React from 'react';
import { useCurrentContext, combineUrls } from '@equinor/fusion';
import { Button, ErrorMessage } from '@equinor/fusion-components';
import * as styles from './styles.less';
import createColumns from './Columns';
import useContracts from './hooks/useContracts';
import GenericFilter from '../../../../components/GenericFilter';
import getFilterSections from './getFilterSections';
import Contract from '../../../../models/contract';
import SortableTable from '../../../../components/SortableTable';

const ContractsOverviewPage = () => {
    const currentProject = useCurrentContext();
    const { contracts, isFetchingContracts, contractsError } = useContracts(currentProject?.id);
    const [filteredContracts, setFilteredContracts] = React.useState<Contract[]>(contracts);

    React.useEffect(() => {
        setFilteredContracts(contracts);
    }, [contracts]);

    const columns = React.useMemo(() => createColumns(), []);
    const filterSections = React.useMemo(() => getFilterSections(contracts), [contracts]);

    const hasError = React.useMemo(
        () => contractsError !== null || (!isFetchingContracts && !contracts.length),
        [contractsError, isFetchingContracts, contractsError]
    );

    return (
        <div className={styles.container}>
            <div className={styles.tableContainer}>
                <div className={styles.toolbar}>
                    <Button relativeUrl={combineUrls(currentProject?.id || '', 'allocate')}>
                        Allocate contract
                    </Button>
                </div>
                <div className={styles.table}>
                    <ErrorMessage hasError={hasError} errorType="notFound" resourceName="contracts">
                        <SortableTable
                            rowIdentifier="contractNumber"
                            data={filteredContracts}
                            isFetching={isFetchingContracts}
                            columns={columns}
                        />
                    </ErrorMessage>
                </div>
            </div>
            {hasError ? null : (
                <GenericFilter
                    data={contracts}
                    filterSections={filterSections}
                    onFilter={setFilteredContracts}
                />
            )}
        </div>
    );
};

export default ContractsOverviewPage;
