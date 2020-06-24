import * as React from 'react';
import * as styles from './styles.less';
import { Button, AddIcon, SyncIcon, DeleteIcon } from '@equinor/fusion-components';
import DelegateAccessSideSheet from '../DelegateAccessSideSheet';

export type AccountType = 'local' | 'external';

type ToolbarButtonProps = {
    icon: React.ReactNode;
    title: string;
    onClick: () => void
};

type ContractAdminTableProps = {
    accountType: AccountType
}

const ToolbarButton: React.FC<ToolbarButtonProps> = ({ icon, title, onClick }) => (
    
    <Button frameless onClick={onClick}>
        <div className={styles.toolbarButton}>
            {icon}
            <span>{title}</span>
        </div>
    </Button>
);

const ContractAdminTable: React.FC<ContractAdminTableProps> = ({accountType}) => {
    const [showDelegateAccess, setShowDelegateAccess] = React.useState<boolean>(false);
    const closeDelegateAccess = React.useCallback(() => setShowDelegateAccess(false), []);
    const openDelegateAccess = React.useCallback(() => setShowDelegateAccess(true), [])

    return (
        <div className={styles.container}>
            <div className={styles.toolbar}>
                <ToolbarButton icon={<AddIcon />} title="Delegate" onClick={openDelegateAccess}/>
                <ToolbarButton icon={<SyncIcon />} title="Re-certify" onClick={() => {}}/>
                <ToolbarButton icon={<DeleteIcon outline/>} title="Remove" onClick={() => {}}/>
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
