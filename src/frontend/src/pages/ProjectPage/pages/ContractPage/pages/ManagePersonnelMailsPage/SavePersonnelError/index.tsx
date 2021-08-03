import { FC, useMemo } from 'react';
import { ContactMailCollection } from '../ManagePersonnelMailContext';
import useSavePersonnelError from './styles';

type SavePersonnelErrorProps = {
    contactMailForm: ContactMailCollection;
};

const SavePersonnelError: FC<SavePersonnelErrorProps> = ({ contactMailForm }) => {
    const inputErrors = useMemo(
        () => contactMailForm.filter((formItem) => !!formItem.inputError),
        [contactMailForm]
    );
    const styles = useSavePersonnelError();
    return (
        <div className={styles.container}>
            {inputErrors.map((inputError, key) => (
                <div className={styles.inputError} key={`save-personnel-error-item-${key}`}>
                    <div className={styles.preferredContactMail}>
                        {inputError.preferredContactMail}
                    </div>
                    <div className={styles.errorDescription}>{inputError.inputError}</div>
                </div>
            ))}
        </div>
    );
};

export default SavePersonnelError;
