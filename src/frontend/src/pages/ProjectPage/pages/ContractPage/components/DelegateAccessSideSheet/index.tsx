import * as React from 'react';
import * as styles from './styles.less';
import { ModalSideSheet, Button } from '@equinor/fusion-components';
import CertifyToPicker from '../CertifiyToPicker';
import classNames from 'classnames';

type DelegateAccessSideSheetProps = {
    showSideSheet: boolean;
    onSideSheetClose: () => void;
    company: string;
};

type DelegationSectionProps = {
    title: string;
    strong?: boolean;
};

const DelegationSection: React.FC<DelegationSectionProps> = ({ title, strong, children }) => (
    <div className={styles.delegationSection}>
        <label
            className={classNames(styles.delegationTitle, {
                [styles.strong]: strong,
            })}
        >
            {title}
        </label>
        {children}
    </div>
);

const DelegateAccessSideSheet: React.FC<DelegateAccessSideSheetProps> = ({
    onSideSheetClose,
    showSideSheet,
    company,
}) => {
    const [delegateTo, setDelegateTo] = React.useState<Date>();
    const onClose = React.useCallback(() => {
        onSideSheetClose();
    }, [onSideSheetClose]);

    const delegateButton = React.useMemo(() => <Button>Delegate</Button>, []);

    return (
        <ModalSideSheet
            show={showSideSheet}
            onClose={onClose}
            header="Delegate access"
            headerIcons={[delegateButton]}
        >
            <div className={styles.delegateContainer}>
                <div className={styles.delegationForm}>
                    <DelegationSection title="Role">
                        <span>{company} Admin Access</span>
                    </DelegationSection>
                    <DelegationSection title="Valid to">
                        <CertifyToPicker onChange={setDelegateTo} defaultSelected="12-months" />
                    </DelegationSection>
                    <DelegationSection title="Add people" strong></DelegationSection>
                </div>

                <div>
                    When delegating responsibility to another person you transfer application rights
                    limited to <strong>adding and removing personnel</strong> and
                    <strong> approving requests</strong>. You can remove them from this role
                    anytime, and extend their role before expiration.
                    <br />
                    <br /> You will be notified before a delegated persons rights expire.
                </div>
            </div>
        </ModalSideSheet>
    );
};
export default DelegateAccessSideSheet;
