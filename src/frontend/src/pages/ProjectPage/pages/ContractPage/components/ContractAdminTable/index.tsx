
import styles from './styles.less';
import {
    Button,
    AddIcon,
    SyncIcon,
    DeleteIcon,
    usePopoverRef,
    DataTable,
    Spinner,
} from '@equinor/fusion-components';
import DelegateAccessSideSheet from '../DelegateAccessSideSheet';
import CertifyToPopover from './components/CertifyToPopover';
import useAccessRemoval from './useAccessRemoval';
import { useAppContext } from '../../../../../../appContext';
import PersonDelegation, {
    PersonDelegationClassification,
} from '../../../../../../models/PersonDelegation';
import { useContractContext } from '../../../../../../contractContex';
import { useCurrentContext } from '@equinor/fusion';
import columns from './columns';
import ToolbarButton from './components/ToolbarButton';
import classNames from 'classnames';
import { FC, useState, useCallback, useEffect } from 'react';

type ContractAdminTableProps = {
    accountType: PersonDelegationClassification;
    admins: PersonDelegation[];
    isFetchingAdmins: boolean;
};

const ContractAdminTable: FC<ContractAdminTableProps> = ({
    accountType,
    admins,
    isFetchingAdmins,
}) => {
    const { apiClient } = useAppContext();
    const { contract } = useContractContext();
    const currentContext = useCurrentContext();

    const [selectedAdmins, setSelectedAdmins] = useState<PersonDelegation[]>([]);
    const [canEdit, setCanEdit] = useState<boolean>(false);
    const [canDelete, setCanDelete] = useState<boolean>(false);

    const [showDelegateAccess, setShowDelegateAccess] = useState<boolean>(false);
    const closeDelegateAccess = useCallback(() => setShowDelegateAccess(false), []);
    const openDelegateAccess = useCallback(() => canEdit && setShowDelegateAccess(true), [
        canEdit,
    ]);

    useEffect(() => {
        const updatedAdmins = admins.filter((admin) =>
            selectedAdmins.some((selectedAdmin) => admin.id === selectedAdmin.id)
        );
        setSelectedAdmins(updatedAdmins);
    }, [admins]);

    const { removeAccess, isRemoving } = useAccessRemoval(accountType, selectedAdmins);

    const removeDelegateAccess = useCallback(() => canDelete && removeAccess(), [
        removeAccess,
        canDelete,
    ]);
    const getPersonAccess = useCallback(
        async (projectId: string, contractId: string) => {
            setCanDelete(false);
            setCanEdit(false);
            const accessHeaders = await apiClient.getDelegationAccessHeaderAsync(
                projectId,
                contractId,
                accountType
            );
            if (accessHeaders.indexOf('POST') !== -1) {
                setCanEdit(true);
            }
            if (accessHeaders.indexOf('DELETE') !== -1) {
                setCanDelete(true);
            }
        },
        [apiClient]
    );

    useEffect(() => {
        const projectId = currentContext?.id;
        const contractId = contract?.id;
        if (contractId && projectId) {
            getPersonAccess(projectId, contractId);
        }
    }, [contract, currentContext]);

    const tableClasses = classNames(styles.table, {
        [styles.emptyTable]: admins.length <= 0 && !isFetchingAdmins,
    });
    return (
        <div className={styles.container}>
            <div className={styles.toolbar}>
                <ToolbarButton
                    icon={<AddIcon />}
                    title="Delegate"
                    onClick={openDelegateAccess}
                    disabled={!canEdit || selectedAdmins.length > 0}
                />
                <CertifyToPopover canEdit={canEdit} admins={selectedAdmins} />
                <ToolbarButton
                    icon={isRemoving ? <Spinner inline /> : <DeleteIcon outline />}
                    title="Remove"
                    onClick={removeDelegateAccess}
                    disabled={!canDelete || selectedAdmins.length <= 0}
                />
            </div>
            <div className={tableClasses}>
                <DataTable
                    data={admins}
                    isFetching={isFetchingAdmins}
                    rowIdentifier="id"
                    columns={columns}
                    isSelectable
                    onSelectionChange={setSelectedAdmins}
                    selectedItems={selectedAdmins}
                />
            </div>

            <DelegateAccessSideSheet
                showSideSheet={showDelegateAccess}
                onSideSheetClose={closeDelegateAccess}
                accountType={accountType}
                canEdit={canEdit}
            />
        </div>
    );
};

export default ContractAdminTable;
