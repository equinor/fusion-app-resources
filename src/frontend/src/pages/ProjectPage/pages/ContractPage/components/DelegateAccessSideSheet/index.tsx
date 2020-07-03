import * as React from 'react';
import * as styles from './styles.less';
import { ModalSideSheet, Button } from '@equinor/fusion-components';
import CertifyToPicker from '../CertifiyToPicker';
import classNames from 'classnames';
import PeopleSelector from '../PeopleSelector';
import { PersonDetails } from '@equinor/fusion';
import { PersonDelegationClassification } from '../../../../../../models/PersonDelegation';

type DelegateAccessSideSheetProps = {
    showSideSheet: boolean;
    onSideSheetClose: () => void;
    accountType: PersonDelegationClassification;
    canEdit: boolean;
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
    accountType,
    canEdit,
}) => {
    const [delegateTo, setDelegateTo] = React.useState<Date>();
    const [selectedPersons, setSelectedPersons] = React.useState<PersonDetails[]>([]);

    const onClose = React.useCallback(() => {
        onSideSheetClose();
    }, [onSideSheetClose]);

    const delegateButton = React.useMemo(() => <Button disabled={!canEdit}>Delegate</Button>, [
        canEdit,
    ]);

    const role = React.useMemo(() => {
        switch (accountType) {
            case 'external':
                return 'External';
            case 'internal':
                return 'Equinor';
            default:
                '';
        }
    }, [accountType]);

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
                        <span>{role} Admin Access</span>
                    </DelegationSection>
                    <DelegationSection title="Valid to">
                        <CertifyToPicker onChange={setDelegateTo} defaultSelected="12-months" />
                    </DelegationSection>
                    <DelegationSection title="Add people" strong>
                        <PeopleSelector
                            selectedPersons={selectedPersons}
                            setSelectedPersons={setSelectedPersons}
                        />
                    </DelegationSection>
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
