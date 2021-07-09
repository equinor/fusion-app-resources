import { CloseCircleIcon, styling, useTooltipRef } from '@equinor/fusion-components';
import { clsx } from '@equinor/fusion-react-styles';
import { ChangeEvent, FC, useCallback, useMemo } from 'react';
import Personnel from '../../../../../../../../../models/Personnel';
import { useManagePersonnelMailContext } from '../../../ManagePersonnelMailContext';
import * as styles from './styles.less';

type PreferredMailProps = {
    item: Personnel;
};

const PreferredMail: FC<PreferredMailProps> = ({ item }) => {
    const { updateContactMail, contactMailForm, isSavingContactMails, checkMailForErrors } =
        useManagePersonnelMailContext();

    const contactFormItem = useMemo(
        () => contactMailForm.find((formItem) => formItem.personnelId === item.personnelId),
        [item, contactMailForm]
    );

    const inputError = useMemo(() => contactFormItem?.inputError, [contactFormItem]);
    const invalidMailTooltip = useTooltipRef(inputError, 'left');

    const input = useMemo(() => contactFormItem?.preferredContactMail || '', [contactFormItem]);

    const onPreferredMailChange = useCallback(
        (input: ChangeEvent<HTMLInputElement>) => {
            if (isSavingContactMails) {
                return;
            }
            const inputValue = input.target.value;
            updateContactMail(item.personnelId, inputValue);
        },
        [updateContactMail, isSavingContactMails]
    );
    const onBlur = useCallback(() => {
        if (!input) {
            return;
        }
        checkMailForErrors(item.personnelId, input);
    }, [checkMailForErrors, input, item.personnelId]);

    const inputClasses = clsx(styles.mailInput, {
        [styles.disabled]: isSavingContactMails,
    });

    return (
        <div className={styles.container}>
            <input
                className={inputClasses}
                value={input}
                onChange={onPreferredMailChange}
                onBlur={onBlur}
            />
            <div className={styles.errorContainer}>
                {inputError && (
                    <div className={styles.error} ref={invalidMailTooltip}>
                        <CloseCircleIcon color={styling.colors.red} />
                    </div>
                )}
            </div>
        </div>
    );
};

export default PreferredMail;
