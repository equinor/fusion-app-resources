import * as React from 'react';
import * as styles from './styles.less';
import { Button, AddIcon, SyncIcon, DeleteIcon, usePopoverRef } from '@equinor/fusion-components';
import DelegateAccessSideSheet from '../DelegateAccessSideSheet';
import CertifyToPopover from './components/CertifyToPopover';
import useAccessRemoval from './useAccessRemoval';
import { useAppContext } from '../../../../../../appContext';
import { PersonDelegationClassification } from '../../../../../../models/PersonDelegation';
import { useContractContext } from '../../../../../../contractContex';
import { useCurrentContext } from '@equinor/fusion';

type ToolbarButtonProps = {
    icon: React.ReactNode;
    title: string;
    onClick?: () => void;
    disabled?: boolean;
};

type ContractAdminTableProps = {
    accountType: PersonDelegationClassification;
};

const ToolbarButton = React.forwardRef<HTMLElement, ToolbarButtonProps>(
    ({ icon, title, onClick, disabled }, ref) => (
        <Button frameless onClick={onClick} ref={ref} disabled={!!disabled}>
            <div className={styles.toolbarButton}>
                {icon}
                <span>{title}</span>
            </div>
        </Button>
    )
);

const ContractAdminTable: React.FC<ContractAdminTableProps> = ({ accountType }) => {
    const { apiClient } = useAppContext();
    const { contract } = useContractContext();
    const currentContext = useCurrentContext();

    const [canEdit, setCanEdit] = React.useState<boolean>(false);
    const [canDelete, setCanDelete] = React.useState<boolean>(false);

    const [showDelegateAccess, setShowDelegateAccess] = React.useState<boolean>(false);
    const closeDelegateAccess = React.useCallback(() => setShowDelegateAccess(false), []);
    const openDelegateAccess = React.useCallback(() => canEdit && setShowDelegateAccess(true), [
        canEdit,
    ]);

    const { removeAccess } = useAccessRemoval(accountType, []);

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
        const contractId = contract?.id;
        const projectId = currentContext?.id;
        if (contractId && projectId) {
            getPersonAccess(projectId, contractId);
        }
    }, [contract, currentContext]);

    const [reCertifyRef] = usePopoverRef(<CertifyToPopover canEdit={canEdit} />, {
        centered: true,
        fillWithContent: true,
        justify: 'center',
    });

    return (
        <div className={styles.container}>
            <div className={styles.toolbar}>
                <ToolbarButton
                    icon={<AddIcon />}
                    title="Delegate"
                    onClick={openDelegateAccess}
                    disabled={!canEdit}
                />
                <ToolbarButton
                    icon={<SyncIcon />}
                    title="Re-certify"
                    ref={reCertifyRef}
                    disabled={!canEdit}
                />
                <ToolbarButton
                    icon={<DeleteIcon outline />}
                    title="Remove"
                    onClick={removeDelegateAccess}
                    disabled={!canDelete}
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
