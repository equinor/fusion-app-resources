import { useState, useEffect, useMemo } from 'react';
import { useCurrentContext, combineUrls } from '@equinor/fusion';
import { Button, ErrorMessage, IconButton, HelpIcon, useTooltipRef } from '@equinor/fusion-components';
import styles from './styles.less';
import createColumns from './Columns';
import useContracts from './hooks/useContracts';
import GenericFilter from '../../../../components/GenericFilter';
import getFilterSections from './getFilterSections';
import Contract from '../../../../models/contract';
import SortableTable from '../../../../components/SortableTable';
import ResourceErrorMessage from '../../../../components/ResourceErrorMessage';
import { Link } from 'react-router-dom';

const ContractsOverviewPage = () => {
    const currentProject = useCurrentContext();
    const { contracts, isFetchingContracts, contractsError } = useContracts(currentProject?.id);
    const [filteredContracts, setFilteredContracts] = useState<Contract[]>(contracts);

    useEffect(() => {
        setFilteredContracts(contracts);
    }, [contracts]);

    const columns = useMemo(() => createColumns(), []);
    const filterSections = useMemo(() => getFilterSections(contracts), [contracts]);

    const hasError = useMemo(
        () => contractsError !== null || (!isFetchingContracts && !contracts.length),
        [contractsError, isFetchingContracts]
    );
    const helpIconRef = useTooltipRef("Help page", "left");

    return (
        <div className={styles.container}>
            <ResourceErrorMessage error={contractsError}>
                <div className={styles.tableContainer}>
                    <div className={styles.toolbar}>
                        <Button id="allocate-contract-btn" relativeUrl={combineUrls(currentProject?.id || '', 'allocate')}>
                            Allocate contract
                        </Button>
                        <Link data-cy="help-btn" target="_blank" to="/help">
                            <IconButton id="help-btn" ref={helpIconRef}>
                                <HelpIcon />
                            </IconButton>
                        </Link>
                    </div>
                    { /** TODO add atributes to component */}
                    <div className={styles.table} data-cy="contract-table-container">
                        <ErrorMessage
                            hasError={hasError}
                            errorType="noData"
                            resourceName="contracts"
                            title="Unfortunately, we did not find any contracts allocated to the selected project"
                        >
                            <SortableTable
                                rowIdentifier="contractNumber"
                                data={filteredContracts}
                                isFetching={isFetchingContracts && !contracts.length}
                                columns={columns}
                            />
                        </ErrorMessage>
                    </div>
                </div>
            </ResourceErrorMessage>
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
