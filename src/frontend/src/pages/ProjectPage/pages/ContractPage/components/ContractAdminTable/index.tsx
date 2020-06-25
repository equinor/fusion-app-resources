import * as React from 'react';
import * as styles from './styles.less';
import { Button, AddIcon, SyncIcon, DeleteIcon, usePopoverRef } from '@equinor/fusion-components';
import DelegateAccessSideSheet from '../DelegateAccessSideSheet';
import CertifyToPopover from './components/CertifyToPopover';
import useAccessRemoval from './useAccessRemoval';

export type AccountType = 'local' | 'external';

type ToolbarButtonProps = {
    icon: React.ReactNode;
    title: string;
    onClick?: () => void;
};

type ContractAdminTableProps = {
    accountType: AccountType;
};

const ToolbarButton = React.forwardRef<HTMLElement, ToolbarButtonProps>(
    ({ icon, title, onClick }, ref) => (
        <Button frameless onClick={onClick} ref={ref}>
            <div className={styles.toolbarButton}>
                {icon}
                <span>{title}</span>
            </div>
        </Button>
    )
);

const ContractAdminTable: React.FC<ContractAdminTableProps> = ({ accountType }) => {
    const [showDelegateAccess, setShowDelegateAccess] = React.useState<boolean>(false);
    const closeDelegateAccess = React.useCallback(() => setShowDelegateAccess(false), []);
    const openDelegateAccess = React.useCallback(() => setShowDelegateAccess(true), []);

    const {removeAccess} = useAccessRemoval(accountType, [])

    const [reCertifyRef] = usePopoverRef(<CertifyToPopover />, {
        centered: true,
        fillWithContent: true,
        justify: 'center',
    });

    return (
        <div className={styles.container}>
            <div className={styles.toolbar}>
                <ToolbarButton icon={<AddIcon />} title="Delegate" onClick={openDelegateAccess} />
                <ToolbarButton
                    icon={<SyncIcon />}
                    title="Re-certify"
                    ref={reCertifyRef}
                />
                <ToolbarButton icon={<DeleteIcon outline />} title="Remove" onClick={removeAccess} />
            </div>
            <DelegateAccessSideSheet
                showSideSheet={showDelegateAccess}
                onSideSheetClose={closeDelegateAccess}
                accountType={accountType}
            />
        </div>
    );
};

export default ContractAdminTable;
