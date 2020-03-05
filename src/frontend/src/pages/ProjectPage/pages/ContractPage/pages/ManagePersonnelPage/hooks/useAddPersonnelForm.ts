import { useCallback } from 'react';
import Person from '../../../../../../../models/Person';
import useForm from '../../../../../../../hooks/useForm';
import { v1 as uuid } from 'uuid';

const createDefaultState = (): Person[] => ([
  {
    personnelId: uuid(),
    name: "",
    phoneNumber: "",
    mail: "",
    jobTitle: ""
  }]);

const useAddPersonnelForm = (defaultState?: Person[] | null) => {
  const validateForm = useCallback((formState: Person[]) => {
    return !formState
      .find(p =>
        Boolean(
          p.name &&
          p.phoneNumber &&
          p.mail) === false)
  }, []);

  return useForm(createDefaultState, validateForm, defaultState);
};

export default useAddPersonnelForm;
