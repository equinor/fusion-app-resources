import { Button, SaveIcon, Spinner } from '@equinor/fusion-components';
import { FC } from 'react';
import { useManagePersonnelMailContext } from '../ManagePersonnelMailContext';
import useStyles from './styles';

const SaveContactMails: FC = () => {
    const styles = useStyles();
    const { isContactMailFormDirty, isSavingContactMails, saveContactMailsAsync } =
        useManagePersonnelMailContext();
    return (
        <div className={styles.container}>
            {isSavingContactMails && <span className={styles.savingText}>Saving, this might take a few moments...</span>}
            <Button disabled={!isContactMailFormDirty} outlined onClick={saveContactMailsAsync}>
                <div className={styles.buttonContainer}>
                    <div>{isSavingContactMails ? <Spinner inline /> : <SaveIcon />}</div>
                    <span className={styles.title}>
                        {isSavingContactMails ? 'Saving...' : 'Save'}
                    </span>
                </div>
            </Button>
        </div>
    );
};

export default SaveContactMails;
