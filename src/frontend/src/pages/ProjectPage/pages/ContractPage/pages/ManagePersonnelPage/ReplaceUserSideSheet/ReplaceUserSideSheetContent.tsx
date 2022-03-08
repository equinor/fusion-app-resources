import { ModalSideSheet, Spinner, PersonPicker, Button } from '@equinor/fusion-components';
import { clsx } from '@equinor/fusion-react-styles';
import Tooltip from '@equinor/fusion-react-tooltip';
import { FC, useMemo } from 'react';
import Personnel from '../../../../../../../models/Personnel';
import { useReplaceUserSideSheetStyles } from './styles';
import useCheckForUpdatedPerson from './useCheckForUpdatedPerson';
import useUpdatePerson from './useUpdatePerson';

type ReplaceUserSideSheetContentProps = {
    person: Personnel;
    onReplacementSuccess: () => void;
};

const ReplaceUserSideSheetContent: FC<ReplaceUserSideSheetContentProps> = ({
    person,
    onReplacementSuccess,
}) => {
    const styles = useReplaceUserSideSheetStyles();
    const { updatedPerson, isCheckingForUpdatedPerson, setUpdatedPerson } =
        useCheckForUpdatedPerson(person);
    const { isUpdatingPerson, updatePerson } = useUpdatePerson(
        updatedPerson,
        person,
        onReplacementSuccess
    );
    const hasDifferentUpn = useMemo(
        () => updatedPerson && updatedPerson.upn !== person.upn,
        [person, updatePerson]
    );
    if (isCheckingForUpdatedPerson) {
        return <Spinner centered />;
    }
    return (
        <div className={styles.container}>
            <span className={styles.infoContainer}>
                Use the input field below to search for the updated account. The input field will search
                in all accounts in Azure Ad.
            </span>
            <span className={styles.infoContainer}>
                The input field can also be used to search for persons with a different UPN
            </span>
            <div className={styles.infoContainer}>
                By updating, all old references to the deleted person account will be replaced with
                the person selected in the text field.
            </div>  
            <div className={styles.infoContainer}>
                <PersonPicker
                    selectedPerson={updatedPerson}
                    onSelect={setUpdatedPerson}
                    initialPerson={updatedPerson}
                />
            </div>

            <div className={styles.accountComparison}>
                <div className={styles.dataField}>
                    <span className={styles.dataTitle}>UNP:</span>
                    <span
                        className={clsx(styles.dataContent, {
                            [styles.oldRef]: hasDifferentUpn,
                        })}
                    >
                        {person.upn}
                    </span>
                    {hasDifferentUpn && (
                        <span className={clsx(styles.dataContent, styles.newRef)}>
                            {updatedPerson?.upn}
                        </span>
                    )}
                </div>
                <div className={styles.dataField}>
                    <span className={styles.dataTitle}>AzureUniqueId:</span>
                    <span
                        className={clsx(styles.dataContent, {
                            [styles.oldRef]: !!updatedPerson,
                        })}
                    >
                        {person.azureUniquePersonId}
                    </span>
                    <span className={clsx(styles.dataContent, styles.newRef)}>
                        {updatedPerson?.azureUniqueId}
                    </span>
                </div>
            </div>

            <div className={styles.updateButton}>
                <Tooltip content="Update person reference to the selected person">
                    <Button onClick={updatePerson} disabled={!updatedPerson}>
                        {isUpdatingPerson ? <Spinner inline /> : 'Update person'}
                    </Button>
                </Tooltip>
                {hasDifferentUpn && (
                    <div className={styles.elevatedAccessInfo}>
                        Note: Changing person UPN requires elevated user access
                    </div>
                )}
            </div>
        </div>
    );
};
export default ReplaceUserSideSheetContent;
