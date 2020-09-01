import * as React from 'react';
import * as styles from './styles.less';
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

type ContractAdminTableProps = {
    accountType: PersonDelegationClassification;
    admins: PersonDelegation[];
    isFetchingAdmins: boolean;
};

const ContractAdminTable: React.FC<ContractAdminTableProps> = ({
    accountType,
    admins,
    isFetchingAdmins,
}) => {
    const { apiClient } = useAppContext();
    const { contract } = useContractContext();
    const currentContext = useCurrentContext();

    const [selectedAdmins, setSelectedAdmins] = React.useState<PersonDelegation[]>([]);
    const [canEdit, setCanEdit] = React.useState<boolean>(false);
    const [canDelete, setCanDelete] = React.useState<boolean>(false);

    const [showDelegateAccess, setShowDelegateAccess] = React.useState<boolean>(false);
    const closeDelegateAccess = React.useCallback(() => setShowDelegateAccess(false), []);
    const openDelegateAccess = React.useCallback(() => canEdit && setShowDelegateAccess(true), [
        canEdit,
    ]);

    React.useEffect(() => {
        const updatedAdmins = admins.filter((admin) =>
            selectedAdmins.some((selectedAdmin) => admin.id === selectedAdmin.id)
        );
        setSelectedAdmins(updatedAdmins);
    }, [admins]);

    const { removeAccess, isRemoving } = useAccessRemoval(accountType, selectedAdmins);

    const removeDelegateAccess = React.useCallback(() => canDelete && removeAccess(), [
        removeAccess,
        canDelete,
    ]);
    const getPersonAccess = React.useCallback(
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

    React.useEffect(() => {
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
