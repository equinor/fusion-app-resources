import { useCallback } from 'react';
import useForm from '../../../../../../../hooks/useForm';
import { v1 as uuid } from 'uuid';
import Personnel from '../../../../../../../models/Personnel';

const createDefaultState = (): Personnel[] => ([
  {
    personnelId: uuid(),
    name: "",
    firstName: "",
    lastName: "",
    phoneNumber: "",
    mail: "",
    jobTitle: "",
    disciplines: []

  }]);

const useAddPersonnelForm = (defaultState?: Personnel[] | null) => {
  const validateForm = useCallback((formState: Personnel[]) => {
    return !formState
      .some(p =>
        !Boolean(
          p.firstName &&
          p.lastName &&
          p.phoneNumber &&
          p.mail))
  }, []);

  return useForm(createDefaultState, validateForm, defaultState);
};

export default useAddPersonnelForm;
